using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public partial class StatsRound : BaseRound
	{
		public override string RoundName => "STATS";
		public override int RoundDuration => 10;

		[Net] public string HiddenName { get; set; }
		[Net] public int HiddenKills { get; set; }
		[Net] public string HiddenHunter { get; set; }
		[Net] public string FirstDeath { get; set; }
		[Net] public string Winner { get; set; }

		private Stats _statsPanel;

		protected override void OnStart()
		{
			Log.Info( "Started Stats Round" );

			if ( Host.IsClient )
			{
				_statsPanel = Sandbox.Hud.CurrentPanel.AddChild<Stats>();

				_statsPanel.Winner.Text = Winner;

				_statsPanel.AddStat( new StatInfo
				{
					Title = "Hidden Kills",
					PlayerName = HiddenName,
					ImageClass = "kills",
					TeamClass = "team_hidden",
					Text = HiddenKills.ToString()
				} );

				_statsPanel.AddStat( new StatInfo
				{
					Title = "Hidden Hunter",
					PlayerName = !string.IsNullOrEmpty( HiddenHunter ) ? HiddenHunter : "N/A",
					ImageClass = "hidden_killer",
					TeamClass = "team_iris",
					Text = ""
				} );

				_statsPanel.AddStat( new StatInfo
				{
					Title = "First Death",
					PlayerName = !string.IsNullOrEmpty( FirstDeath ) ? FirstDeath : "N/A",
					ImageClass = "first_death",
					TeamClass = "team_iris",
					Text = ""
				} );
			}
		}

		protected override void OnFinish()
		{
			Log.Info( "Finished Stats Round" );

			if ( _statsPanel != null )
			{
				_statsPanel.Delete( );
			}
		}

		protected override void OnTimeUp()
		{
			Log.Info( "Stats Time Up!" );

			Game.Instance.ChangeRound( new HideRound() );

			base.OnTimeUp();
		}

		public override void OnPlayerSpawn( Player player )
		{
			if ( Players.Contains( player ) ) return;

			player.MakeSpectator();

			AddPlayer( player );

			base.OnPlayerSpawn( player );
		}
	}
}
