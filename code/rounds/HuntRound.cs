using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
    public class HuntRound : BaseRound
	{
		public override string RoundName => "HUNT";
		public override int RoundDuration => 300;
		public override bool CanPlayerSuicide => true;

		public List<Player> Spectators = new();

		private string _hiddenHunter;
		private string _firstDeath;
		private bool _isGameOver;
		private int _hiddenKills;

		public override void OnPlayerKilled( Player player )
		{
			Players.Remove( player );
			Spectators.Add( player );

			player.MakeSpectator( player.EyePosition );

			if ( player.Team is HiddenTeam )
			{
				if ( player.LastAttacker is Player attacker )
				{
					_hiddenHunter = attacker.Client.Name;
				}

				_ = LoadStatsRound( "I.R.I.S. Eliminated The Hidden" );

				return;
			}
			else
			{
				if ( string.IsNullOrEmpty( _firstDeath ) )
				{
					_firstDeath = player.Client.Name;
				}

				_hiddenKills++;
			}

			if ( Players.Count <= 1 )
			{
				_ = LoadStatsRound( "The Hidden Eliminated I.R.I.S." );
			}
		}

		public override void OnPlayerLeave( Player player )
		{
			base.OnPlayerLeave( player );

			Spectators.Remove( player );

			if ( player.Team is HiddenTeam )
			{
				_ = LoadStatsRound( "The Hidden Disconnected" );
			}
		}

		public override void OnPlayerSpawn( Player player )
		{
			player.MakeSpectator();

			Spectators.Add( player );
			Players.Remove( player );

			base.OnPlayerSpawn( player );
		}

		protected override void OnStart()
		{
			Log.Info( "Started Hunt Round" );

			if ( Host.IsServer )
			{
				foreach ( var client in Client.All )
				{
					if ( client.Pawn is Player player )
						SupplyLoadouts( player );
				}
			}
		}

		protected override void OnFinish()
		{
			Log.Info( "Finished Hunt Round" );

			if ( Host.IsServer )
			{
				Spectators.Clear();
			}
		}

		protected override void OnTimeUp()
		{
			if ( _isGameOver ) return;

			Log.Info( "Hunt Time Up!" );

			_ = LoadStatsRound( "I.R.I.S. Survived Long Enough" );

			base.OnTimeUp();
		}

		private void SupplyLoadouts( Player player )
		{
			// Give everyone who is alive their starting loadouts.
			if ( player.Team != null && player.LifeState == LifeState.Alive )
			{
				player.Team.SupplyLoadout( player );
				AddPlayer( player );
			}
		}

		private async Task LoadStatsRound(string winner, int delay = 3)
		{
			_isGameOver = true;

			await Task.Delay( delay * 1000 );

			if ( Game.Instance.Round != this )
				return;

			var hidden = Game.Instance.GetTeamPlayers<HiddenTeam>().FirstOrDefault();
			var hiddenName = hidden != null ? hidden.Client.Name : "";

			Game.Instance.ChangeRound( new StatsRound
			{
				HiddenName = hiddenName,
				HiddenKills = _hiddenKills,
				FirstDeath = _firstDeath,
				HiddenHunter = _hiddenHunter,
				Winner = winner
			} );
		}
	}
}
