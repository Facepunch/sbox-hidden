
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Collections.Generic;

namespace HiddenGamemode
{
	public class Scoreboard : Panel
	{
		public struct TeamSection
		{
			public Label TeamName;
			public Panel TeamIcon;

			public Panel TeamContainer;

			public Panel Header;
			public Panel TeamHeader;
			public Panel Canvas;
		}

		public TextEntry Input { get; protected set; }
		public Dictionary<int, ScoreboardEntry> Entries = new();
		public Dictionary<int, TeamSection> TeamSections = new();

		public Panel ScoreboardHeader;
		public Label ScoreboardTitle;


		public Scoreboard()
		{
			StyleSheet.Load( "/ui/Scoreboard.scss" );

			AddClass( "scoreboard" );

			ScoreboardHeader = Add.Panel( "scoreboard-header" );
			ScoreboardTitle = ScoreboardHeader.Add.Label( "SCOREBOARD" );

			AddTeamHeader( Game.Instance.HiddenTeam );
			AddTeamHeader( Game.Instance.IrisTeam );

			PlayerScore.OnPlayerAdded += AddPlayer;
			PlayerScore.OnPlayerUpdated += UpdatePlayer;
			PlayerScore.OnPlayerRemoved += RemovePlayer;

			foreach ( var player in PlayerScore.All )
			{
				AddPlayer( player );
			}
		}

		public override void Tick()
		{
			base.Tick();
			
			SetClass( "open", Local.Client?.Input.Down( InputButton.Score ) ?? false );
		}

		protected void AddTeamHeader(BaseTeam team)
		{
			var section = new TeamSection
			{
				
			};

			// Set up the Container for the Team on the scoreboard
			section.TeamContainer = Add.Panel( "team-container" );
			section.TeamHeader = section.TeamContainer.Add.Panel( "team-header" );
			section.Header = section.TeamContainer.Add.Panel( "table-header" );
			section.Canvas = section.TeamContainer.Add.Panel( "canvas" );

			section.TeamIcon = section.TeamHeader.Add.Panel( "teamIcon" );
			section.TeamName = section.TeamHeader.Add.Label( team.Name, "teamName" );
			
			section.TeamIcon.AddClass( team.HudClassName );

			section.Header.Add.Label( "NAME", "name" );
			section.Header.Add.Label( "KILLS", "kills" );
			section.Header.Add.Label( "DEATHS", "deaths" );
			section.Header.Add.Label( "PING", "ping" );

			section.Canvas.AddClass( team.HudClassName );
			section.Header.AddClass( team.HudClassName );
			section.TeamHeader.AddClass( team.HudClassName );

			TeamSections[team.Index] = section;
		}

		protected void AddPlayer( PlayerScore.Entry entry )
		{
			var teamIndex = entry.Get( "team", 0 );

			if ( !TeamSections.TryGetValue( teamIndex, out var section ) )
			{
				section = TeamSections[ Game.Instance.IrisTeam.Index ];
			}

			var p = section.Canvas.AddChild<ScoreboardEntry>();
			p.UpdateFrom( entry );
			Entries[entry.Id] = p;
		}

		protected void UpdatePlayer( PlayerScore.Entry entry )
		{
			if ( Entries.TryGetValue( entry.Id, out var panel ) )
			{
				var currentTeamIndex = 0;
				var newTeamIndex = entry.Get( "team", 0 );

				foreach (var kv in TeamSections)
				{
					if ( kv.Value.Canvas == panel.Parent )
					{
						currentTeamIndex = kv.Key;
					}
				}

				if ( currentTeamIndex != newTeamIndex )
				{
					panel.Parent = TeamSections[newTeamIndex].Canvas;
				}

				panel.UpdateFrom( entry );
			}
		}

		protected void RemovePlayer( PlayerScore.Entry entry )
		{
			if ( Entries.TryGetValue( entry.Id, out var panel ) )
			{
				panel.Delete();
				Entries.Remove( entry.Id );
			}
		}
	}

	public class ScoreboardEntry : Sandbox.UI.ScoreboardEntry
	{
		public ScoreboardEntry()
		{

		}

		public override void UpdateFrom( PlayerScore.Entry entry )
		{
			base.UpdateFrom( entry );


		}
	}
}
