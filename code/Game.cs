﻿using Sandbox;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	[ClassLibrary( "sbox-hidden", Title = "Hidden" )]
	partial class Game : Sandbox.Game
	{
		internal static HiddenTeam HiddenTeam;
		internal static IrisTeam IrisTeam;

		public static Game Instance
		{
			get => Current as Game;
		}

		[NetPredicted] public BaseRound Round { get; private set; }

		[ServerVar( "hdn_min_players", Help = "The minimum players required to start.", Name = "Minimum Players" )]
		private int _minPlayers => 2;

		public Game()
		{
			if ( IsServer )
			{
				_ = new Hud();
			}
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
				Round?.OnSecond();
			}
		}

		public override void DoPlayerNoclip( Sandbox.Player player )
		{
			// Do nothing. The player can't noclip in this mode.
		}

		public override void DoPlayerSuicide( Sandbox.Player player )
		{
			// Do nothing. The player can't suicide in this mode.
			base.DoPlayerSuicide( player );
		}

		public override void PostLevelLoaded()
		{
			HiddenTeam = new();
			IrisTeam = new();

			_ = StartSecondTimer();

			base.PostLevelLoaded();
		}

		public override void PlayerKilled( Sandbox.Player player )
		{
			Round?.OnPlayerKilled( player as Player );

			base.PlayerKilled( player );
		}

		public override void PlayerJoined( Sandbox.Player player )
		{
			Log.Info( player.Name + " joined, checking minimum player count..." );

			CheckMinimumPlayers();

			base.PlayerJoined( player );
		}

		public override void PlayerDisconnected( Sandbox.Player player, NetworkDisconnectionReason reason )
		{
			Log.Info( player.Name + " left, checking minimum player count..." );

			Round?.OnPlayerLeave( player as Player );

			CheckMinimumPlayers();

			base.PlayerDisconnected( player, reason );
		}

		public override Player CreatePlayer() => new();

		private void CheckMinimumPlayers()
		{
			if ( Sandbox.Player.All.Count >= _minPlayers)
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
