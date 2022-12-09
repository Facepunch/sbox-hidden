using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
    public class HuntRound : BaseRound
	{
		public override string RoundName => "HUNT";
		public override int RoundDuration => 300;
		public override bool CanPlayerSuicide => true;

		public List<HiddenPlayer> Spectators = new();

		private string HiddenHunter;
		private string FirstDeath;
		private bool IsGameOver;
		private int HiddenKills;

		[ConCmd.Admin( "hdn_end_round" )]
		public static void EndRoundCmd()
		{
			if ( Game.Instance.Round is HuntRound round )
			{
				round.RoundEndTime = 0f;
				round.OnTimeUp();
			}
		}

		public override void OnPlayerKilled( HiddenPlayer player )
		{
			Players.Remove( player );
			Spectators.Add( player );

			player.MakeSpectator( player.EyePosition );

			if ( player.Team is HiddenTeam )
			{
				if ( player.LastAttacker is HiddenPlayer attacker )
				{
					HiddenHunter = attacker.Client.Name;
				}

				_ = LoadStatsRound( "I.R.I.S. Eliminated The Hidden" );

				return;
			}
			else
			{
				if ( string.IsNullOrEmpty( FirstDeath ) )
				{
					FirstDeath = player.Client.Name;
				}

				HiddenKills++;
			}

			if ( Players.Count <= 1 )
			{
				_ = LoadStatsRound( "The Hidden Eliminated I.R.I.S." );
			}
		}

		public override void OnPlayerLeave( HiddenPlayer player )
		{
			base.OnPlayerLeave( player );

			Spectators.Remove( player );

			if ( player.Team is HiddenTeam )
			{
				_ = LoadStatsRound( "The Hidden Disconnected" );
			}
		}

		public override void OnPlayerSpawn( HiddenPlayer player )
		{
			player.MakeSpectator();

			Spectators.Add( player );
			Players.Remove( player );

			base.OnPlayerSpawn( player );
		}

		protected override void OnStart()
		{
			if ( Host.IsServer )
			{
				foreach ( var client in Client.All )
				{
					if ( client.Pawn is HiddenPlayer player )
						SupplyLoadouts( player );
				}
			}
		}

		protected override void OnFinish()
		{
			if ( Host.IsServer )
			{
				Spectators.Clear();
			}
		}

		protected override void OnTimeUp()
		{
			if ( IsGameOver ) return;

			_ = LoadStatsRound( "I.R.I.S. Survived Long Enough" );

			base.OnTimeUp();
		}

		private void SupplyLoadouts( HiddenPlayer player )
		{
			// Give everyone who is alive their starting loadouts.
			if ( player.Team != null && player.LifeState == LifeState.Alive )
			{
				player.Team.SupplyLoadout( player );
				AddPlayer( player );
			}
		}

		private async Task LoadStatsRound( string winner, int delay = 3 )
		{
			IsGameOver = true;

			await Task.Delay( delay * 1000 );

			if ( Game.Instance.Round != this )
				return;

			var hidden = Game.Instance.GetTeamPlayers<HiddenTeam>().FirstOrDefault();
			var hiddenName = hidden != null ? hidden.Client.Name : "";

			Game.Instance.ChangeRound( new StatsRound
			{
				HiddenName = hiddenName,
				HiddenKills = HiddenKills,
				FirstDeath = FirstDeath,
				HiddenHunter = HiddenHunter,
				Winner = winner
			} );
		}
	}
}
