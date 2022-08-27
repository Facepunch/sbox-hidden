using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facepunch.Hidden
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

		private Stats StatsPanel;

		protected override void OnStart()
		{
			if ( Host.IsClient )
			{
				StatsPanel = Local.Hud.AddChild<Stats>();

				StatsPanel.Winner.Text = Winner;

				StatsPanel.AddStat( new StatInfo
				{
					Title = "Hidden Kills",
					PlayerName = HiddenName,
					ImageClass = "kills",
					TeamClass = "team_hidden",
					Text = HiddenKills.ToString()
				} );

				StatsPanel.AddStat( new StatInfo
				{
					Title = "Hidden Hunter",
					PlayerName = !string.IsNullOrEmpty( HiddenHunter ) ? HiddenHunter : "N/A",
					ImageClass = "hidden_killer",
					TeamClass = "team_iris",
					Text = ""
				} );

				StatsPanel.AddStat( new StatInfo
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
			if ( StatsPanel != null )
			{
				StatsPanel.Delete( );
			}
		}

		protected override void OnTimeUp()
		{
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
