
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
			StyleSheet.Load( "/ui/Stats.scss" );
			Container = Add.Panel( "statsContainer" );
			Winner = Add.Label( "Winner", "winner" );
		}

		public void AddStat( StatInfo info )
		{
			var panel = Container.Add.Panel( "item" );
			panel.Add.Label( info.Title, "title" );

			panel.Add.Label( info.PlayerName, "playerName" )
				.AddClass( info.TeamClass );

			panel.Add.Label( info.Text, "text" );

			panel.Add.Panel( "icon" )
				.AddClass( info.ImageClass );

			panel.AddClass( info.TeamClass );
		}
	}
}
