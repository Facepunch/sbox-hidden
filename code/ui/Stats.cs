
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace HiddenGamemode
{
	public struct StatInfo
	{
		public string Title;
		public string PlayerName;
		public string ImageClass;
		public string TeamClass;
		public string Text;
	}

	public class Stats : Panel
	{
		public Panel Container;
		public Label Winner;

		public Stats()
		{
			StyleSheet.Load( "/ui/stats.scss" );
			Container = Add.Panel( "statsContainer" );
			Winner = Add.Label( "Winner", "winner" );
		}

		public void AddStat( StatInfo info )
		{
			var panel = Container.Add.Panel( "statPanel" );
			panel.Add.Label( info.Title, "statTitle" );

			panel.Add.Label( info.PlayerName, "statPlayerName" )
				.AddClass( info.TeamClass );

			panel.Add.Label( info.Text, "statText" );
			panel.AddClass( info.ImageClass );
			panel.AddClass( info.TeamClass );
		}
	}
}
