using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	[Library( "hidden", Title = "Hidden" )]
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

		[Net] public BaseRound Round { get; private set; }

		private BaseRound _lastRound;
		private List<BaseTeam> _teams;

		[ServerVar( "hdn_min_players", Help = "The minimum players required to start." )]
		public static int MinPlayers { get; set; } = 2;

		[ServerVar( "hdn_friendly_fire", Help = "Whether or not friendly fire is enabled." )]
		public static bool AllowFriendlyFire { get; set; } = true;

		[ServerVar( "hdn_voice_radius", Help = "How far away players can hear eachother talk." )]
		public static int VoiceRadius { get; set; } = 2048;

		public Game()
		{
			_teams = new();

			if ( IsServer )
			{
				Hud = new();
			}

			LightFlickers = new();
			HiddenTeam = new();
			IrisTeam = new();

			AddTeam( HiddenTeam );
			AddTeam( IrisTeam );

			_ = StartTickTimer();
		}

		public void AddTeam( BaseTeam team )
		{
			_teams.Add( team );
			team.Index = _teams.Count;
		}

		public BaseTeam GetTeamByIndex( int index )
		{
			return _teams[index - 1];
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
			while (true)
			{
				await Task.DelaySeconds( 1 );
				OnSecond();
			}
		}

		public async Task StartTickTimer()
		{
			while (true)
			{
				await Task.NextPhysicsFrame();
				OnTick();
			}
		}

		public override void DoPlayerNoclip( Client client )
		{
			// Do nothing. The player can't noclip in this mode.
		}

		public override void DoPlayerSuicide( Client client )
		{
			if ( client.Pawn.LifeState == LifeState.Alive && Round?.CanPlayerSuicide == true )
			{
				// This simulates the player being killed.
				client.Pawn.LifeState = LifeState.Dead;
				client.Pawn.OnKilled();
				OnKilled( client.Pawn );
			}
		}

		public override void PostLevelLoaded()
		{
			_ = StartSecondTimer();

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
			Log.Info( client.Name + " left, checking minimum player count..." );

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

		private void OnTick()
		{
			Round?.OnTick();

			for ( var i = 0; i < _teams.Count; i++ )
			{
				_teams[i].OnTick();
			}

			LightFlickers?.OnTick();

			if ( IsClient )
			{
				// We have to hack around this for now until we can detect changes in net variables.
				if ( _lastRound != Round )
				{
					_lastRound?.Finish();
					_lastRound = Round;
					_lastRound.Start();
				}

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
