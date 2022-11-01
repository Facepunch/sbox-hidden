using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
	partial class Game : Sandbox.Game
	{
		public LightFlickers LightFlickers { get; set; }
		public HiddenTeam HiddenTeam { get; set; }
		public IrisTeam IrisTeam { get; set; }
		public Hud Hud { get; set; }

		public static Game Instance
		{
			get => Current as Game;
		}

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

		public Game()
		{
			Teams = new();

			if ( IsServer )
			{
				Hud = new();
			}

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

		public IEnumerable<Player> GetTeamPlayers<T>( bool isAlive = false ) where T : BaseTeam
		{
			var output = Client.All
				.Where( c => c.Pawn is Player player && player.Team is T )
				.Select( c => c.Pawn as Player );

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

		public override void DoPlayerNoclip( Client client )
		{
			// Do nothing. The player can't noclip in this mode.
		}

		public override void DoPlayerSuicide( Client client )
		{
			// Do nothing. The player can't suicide in this mode.
		}

		public override bool CanHearPlayerVoice( Client source, Client dest )
		{
			if ( !source.IsValid() || !dest.IsValid() )
				return false;

			if ( !source.Pawn.IsValid() || !dest.Pawn.IsValid() )
				return false;

			if ( source.Pawn.LifeState == LifeState.Dead )
				return false;

			// Don't play our own voice back to us.
			if ( source == dest )
				return false;

			return source.Pawn.Position.Distance( dest.Pawn.Position ) <= VoiceRadius;
		}

		public override void Simulate( Client cl )
		{
			if ( cl.Pawn.IsValid() && Input.Down( InputButton.Voice ) && cl.Pawn.LifeState == LifeState.Alive )
			{
				// Show a voice list entry if we're holding down the voice button.
				VoiceList.Current?.OnVoicePlayed( cl.PlayerId, 0.5f );
			}

			base.Simulate( cl );
		}

		public override void OnVoicePlayed( Client client )
		{
			client.VoiceStereo = false;

			base.OnVoicePlayed( client );
		}

		public override void PostLevelLoaded()
		{
			base.PostLevelLoaded();
		}

		public override void OnKilled( Entity entity)
		{
			if ( entity is Player player )
				Round?.OnPlayerKilled( player );

			base.OnKilled( entity);
		}

		public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
		{
			Round?.OnPlayerLeave( client.Pawn as Player );
			base.ClientDisconnect( client, reason );
		}

		public override void RenderHud()
		{
			var pawn = Local.Pawn as Player;
			if ( !pawn.IsValid() ) return;

			pawn.RenderHud( Screen.Size );

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
			if ( pawn is not Player player )
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

		public override void ClientJoined( Client client )
		{
			var pawn = new Player();
			client.Pawn = pawn;
			pawn.UniqueRandomSeed = Rand.Int( 0, 999999 );
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

		private void OnRoundChanged( BaseRound oldRound, BaseRound newRound )
		{
			oldRound?.Finish();
			newRound?.Start();
		}

		private bool IsValidSpawnpoint( Player player, TeamSpawnpoint spawnpoint )
		{
			if ( player.Team is HiddenTeam )
				return spawnpoint.Team == TeamSpawnpoint.TeamType.Hidden;

			if ( player.Team is IrisTeam )
				return spawnpoint.Team == TeamSpawnpoint.TeamType.IRIS;

			return false;
		}

		private void CheckMinimumPlayers()
		{
			if ( Client.All.Count >= MinPlayers)
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
