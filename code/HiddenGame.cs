using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Effects;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Hidden
{
	public partial class HiddenGame : GameManager
	{
		public LightFlickers LightFlickers { get; set; }
		public HiddenTeam HiddenTeam { get; set; }
		public IrisTeam IrisTeam { get; set; }
		public Hud Hud { get; set; }

		public static HiddenGame Entity => Current as HiddenGame;

		[Net, Change( nameof( OnRoundChanged ) )] public BaseRound Round { get; private set; }

		private List<BaseTeam> Teams;

		[ConVar.Server( "hdn_min_players", Help = "The minimum players required to start." )]
		public static int MinPlayers { get; set; } = 2;

		[ConVar.Server( "hdn_friendly_fire", Help = "Whether or not friendly fire is enabled." )]
		public static bool FriendlyFire { get; set; } = true;

		[ConVar.Server( "hdn_scale_hidden_damage", Help = "Scale the damage taken by the Hidden." )]
		public static float ScaleHiddenDamage { get; set; } = 0.5f;

		[ConVar.Replicated( "hdn_sense", Help = "Whether or not The Hidden can use their Sense ability." )]
		public static bool CanUseSense { get; set; } = true;

		[ConVar.Replicated( "hdn_charge_attack", Help = "Whether or not The Hidden can use their charge attack." )]
		public static bool CanUseChargeAttack { get; set; } = true;

		[ConVar.Server( "hdn_voice_radius", Help = "How far away players can hear eachother talk." )]
		public static int VoiceRadius { get; set; } = 2048;

		private RealTimeUntil NextSecondTime { get; set; }
		private ScreenEffects HealthPostProcessing { get; set; }
		private ScreenEffects ImmersionPostProcessing { get; set; }
		private Color OverlayColor { get; set; } = Color.Orange;
		private float BlurAmount { get; set; } = 0f;

		public HiddenGame() : base()
		{
			Teams = new();

			LightFlickers = new();
			HiddenTeam = new();
			IrisTeam = new();

			AddTeam( HiddenTeam );
			AddTeam( IrisTeam );

			NextSecondTime = 0f;
		}

		public void AddTeam( BaseTeam team )
		{
			Teams.Add( team );
			team.Index = Teams.Count;
		}

		public BaseTeam GetTeamByIndex( int index )
		{
			return Teams[index - 1];
		}

		public IEnumerable<HiddenPlayer> GetTeamPlayers<T>( bool isAlive = false ) where T : BaseTeam
		{
			var output = Game.Clients
				.Where( c => c.Pawn is HiddenPlayer player && player.Team is T )
				.Select( c => c.Pawn as HiddenPlayer );

			if ( isAlive )
				output = output.Where( p => p.LifeState == LifeState.Alive );

			return output;
		}

		public void ChangeRound(BaseRound round)
		{
			Assert.NotNull( round );

			Round?.Finish();
			Round = round;
			Round?.Start();
		}

		public override void ClientSpawn()
		{
			Game.RootPanel?.Delete( true );
			Game.RootPanel = new Hud();

			ImmersionPostProcessing = new();
			HealthPostProcessing = new();

			Camera.Main.RemoveAllHooks();
			Camera.Main.AddHook( ImmersionPostProcessing );
			Camera.Main.AddHook( HealthPostProcessing );

			base.ClientSpawn();
		}

		public override bool CanHearPlayerVoice( IClient src, IClient dest )
		{
			if ( !src.IsValid() || !dest.IsValid() )
				return false;

			if ( !src.Pawn.IsValid() || !dest.Pawn.IsValid() )
				return false;

			var srcPawn = src.Pawn as Entity;
			if ( srcPawn.LifeState == LifeState.Dead )
				return false;

			// Don't play our own voice back to us.
			if ( src == dest )
				return false;

			return src.Pawn.Position.Distance( dest.Pawn.Position ) <= VoiceRadius;
		}

		public override void Simulate( IClient cl )
		{
			var player = cl.Pawn as HiddenPlayer;

			if ( player.IsValid() && Input.Down( InputButton.Voice ) && player.LifeState == LifeState.Alive )
			{
				// Show a voice list entry if we're holding down the voice button.
				VoiceList.Current?.OnVoicePlayed( cl.SteamId, 0.5f );
			}

			base.Simulate( cl );
		}

		public override void OnVoicePlayed( IClient client )
		{
			client.Voice.WantsStereo = false;

			base.OnVoicePlayed( client );
		}

		public override void PostLevelLoaded()
		{
			base.PostLevelLoaded();
		}

		public override void OnKilled( Entity entity)
		{
			if ( entity is HiddenPlayer player )
				Round?.OnPlayerKilled( player );

			base.OnKilled( entity);
		}

		public override void ClientDisconnect( IClient client, NetworkDisconnectionReason reason )
		{
			Round?.OnPlayerLeave( client.Pawn as HiddenPlayer );
			base.ClientDisconnect( client, reason );
		}

		public override void RenderHud()
		{
			var player = HiddenPlayer.Me;
			if ( !player.IsValid() ) return;

			player.RenderHud( Screen.Size );

			foreach ( var entity in All.OfType<IHudRenderer>() )
			{
				if ( entity.IsValid() )
				{
					entity.RenderHud( Screen.Size );
				}
			}
		}

		public override void MoveToSpawnpoint( Entity pawn )
		{
			if ( pawn is not HiddenPlayer player )
			{
				base.MoveToSpawnpoint( pawn );
				return;
			}

			var spawnpoint = All.OfType<TeamSpawnpoint>()
				.OrderBy( x => Guid.NewGuid() )
				.Where( x => IsValidSpawnpoint( player, x ) )
				.FirstOrDefault();

			if ( spawnpoint == null )
			{
				base.MoveToSpawnpoint( pawn );
				return;
			}

			pawn.Transform = spawnpoint.Transform;
		}

		public override void ClientJoined( IClient client )
		{
			var pawn = new HiddenPlayer();
			client.Pawn = pawn;
			pawn.UniqueRandomSeed = Game.Random.Int( 0, 999999 );
			pawn.Respawn();

			base.ClientJoined( client );
		}

		private void OnSecond()
		{
			CheckMinimumPlayers();
			Round?.OnSecond();
		}

		[Event.Tick]
		private void OnTick()
		{
			Round?.OnTick();

			for ( var i = 0; i < Teams.Count; i++ )
			{
				Teams[i].OnTick();
			}

			if ( NextSecondTime )
			{
				OnSecond();
				NextSecondTime = 1f;
			}

			LightFlickers?.OnTick();
		}

		[Event.Client.Frame]
		private void OnFrame()
		{
			if ( Game.LocalPawn is not HiddenPlayer player )
				return;

			var isHiddenTeam = player.Team is HiddenTeam;

			if ( isHiddenTeam )
			{
				ImmersionPostProcessing.ChromaticAberration.Scale = 0.75f;
				ImmersionPostProcessing.ChromaticAberration.Offset = new Vector3( 0.002f, 0f, 0.002f );
			}
			else
			{
				ImmersionPostProcessing.ChromaticAberration.Scale = 0f;
			}

			ImmersionPostProcessing.Sharpen = 0.1f;

			if ( isHiddenTeam )
			{
				ImmersionPostProcessing.Saturation = 1f;
				ImmersionPostProcessing.FilmGrain.Intensity = 0f;

				ImmersionPostProcessing.Vignette.Intensity = 0.3f;
				ImmersionPostProcessing.Vignette.Color = Color.Red.Darken( 0.3f ).WithAlpha( 0.9f );
				ImmersionPostProcessing.Vignette.Smoothness = 0.9f;
				ImmersionPostProcessing.Vignette.Roundness = 0.5f;

				if ( player.IsSenseActive )
				{
					OverlayColor = Color.Lerp( OverlayColor, Color.Red, Time.Delta * 4f );
					BlurAmount = BlurAmount.LerpTo( 0f, Time.Delta * 4f );
				}
				else
				{
					OverlayColor = Color.Lerp( OverlayColor, Color.Orange, Time.Delta * 4f );
					BlurAmount = BlurAmount.LerpTo( 0f, Time.Delta * 4f );
				}
			}
			else
			{
				ImmersionPostProcessing.Saturation = 0.9f;

				ImmersionPostProcessing.Vignette.Intensity = 0.6f;
				ImmersionPostProcessing.Vignette.Color = Color.Black.WithAlpha( 0.5f );
				ImmersionPostProcessing.Vignette.Smoothness = 0.9f;
				ImmersionPostProcessing.Vignette.Roundness = 0.7f;

				ImmersionPostProcessing.FilmGrain.Response = 0.1f;
				ImmersionPostProcessing.FilmGrain.Intensity = 0.04f;
			}

			if ( player.IsSpectator && !isHiddenTeam )
			{
				ImmersionPostProcessing.Saturation = 0f;

				ImmersionPostProcessing.FilmGrain.Response = 0.2f;
				ImmersionPostProcessing.FilmGrain.Intensity = 0.05f;

				HealthPostProcessing.Vignette.Intensity = 0f;
			}
			else
			{
				var healthScale = (0.4f / 100f) * player.Health;
				HealthPostProcessing.Saturation = 0.6f + healthScale;

				HealthPostProcessing.Vignette.Intensity = 0.5f - healthScale * 2f;
				HealthPostProcessing.Vignette.Color = Color.Red.Darken( 0.8f ).WithAlpha( 0.5f );
				HealthPostProcessing.Vignette.Smoothness = 0.9f;
				HealthPostProcessing.Vignette.Roundness = 0.5f;
			}

			var sum = ScreenShake.List.OfType<ScreenShake.Random>().Sum( s => (1f - s.Progress) );

			ImmersionPostProcessing.Pixelation = 0.02f * sum;
			ImmersionPostProcessing.ChromaticAberration.Scale += (0.05f * sum);
		}

		private void OnRoundChanged( BaseRound oldRound, BaseRound newRound )
		{
			oldRound?.Finish();
			newRound?.Start();
		}

		private bool IsValidSpawnpoint( HiddenPlayer player, TeamSpawnpoint spawnpoint )
		{
			if ( player.Team is HiddenTeam )
				return spawnpoint.Team == TeamSpawnpoint.TeamType.Hidden;

			if ( player.Team is IrisTeam )
				return spawnpoint.Team == TeamSpawnpoint.TeamType.IRIS;

			return false;
		}

		private void CheckMinimumPlayers()
		{
			if ( Game.Clients.Count >= MinPlayers)
			{
				if ( Round is LobbyRound || Round == null )
				{
					ChangeRound( new HideRound() );
				}
			}
			else if ( Round is not LobbyRound )
			{
				ChangeRound( new LobbyRound() );
			}
		}
	}
}
