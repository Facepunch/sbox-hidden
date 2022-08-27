using Sandbox;
using System.Collections.Generic;
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

		[ConVar.Server( "hdn_voice_radius", Help = "How far away players can hear eachother talk." )]
		public static int VoiceRadius { get; set; } = 2048;

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

			_ = StartSecondTimer();
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

		public List<Player> GetTeamPlayers<T>(bool isAlive = false) where T : BaseTeam
		{
			var output = new List<Player>();

			foreach ( var client in Client.All )
			{
				if ( client.Pawn is Player player && player.Team is T )
				{
					if ( !isAlive || player.LifeState == LifeState.Alive )
					{
						output.Add( player );
					}
				}
			}

			return output;
		}

		public void ChangeRound(BaseRound round)
		{
			Assert.NotNull( round );

			Round?.Finish();
			Round = round;
			Round?.Start();
		}

		public async Task StartSecondTimer()
		{
			while ( true )
			{
				try
				{
					await Task.DelaySeconds( 1 );
					OnSecond();
				}
				catch( TaskCanceledException _ )
				{
					break;
				}
			}
		}

		public override void DoPlayerNoclip( Client client )
		{
			// Do nothing. The player can't noclip in this mode.
		}

		public override void DoPlayerSuicide( Client client )
		{
			// Do nothing. The player can't suicide in this mode.
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

		public override void ClientJoined( Client client )
		{
			var pawn = new Player();
			client.Pawn = pawn;
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

			LightFlickers?.OnTick();

			if ( IsClient )
			{
				foreach ( var client in Client.All )
				{
					if ( client.Pawn is not Player player ) return;

					if ( player.TeamIndex != player.LastTeamIndex )
					{
						player.Team = GetTeamByIndex( player.TeamIndex );
						player.LastTeamIndex = player.TeamIndex;
					}
				};
			}
		}

		private void OnRoundChanged( BaseRound oldRound, BaseRound newRound )
		{
			oldRound?.Finish();
			newRound.Start();
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
