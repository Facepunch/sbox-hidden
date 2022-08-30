using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
	public partial class HideRound : BaseRound
	{
		[ConVar.Server( "hdn_host_team", Help = "Make the host always this team." )]
		public static string HostTeam { get; set; }

		public override bool CanPlayerTakeDamage => false;
		public override string RoundName => "PREPARE";
		public override int RoundDuration => 20;

		private Deployment DeploymentPanel;
		private bool RoundStarted;

		[ConCmd.Server( "hdn_select_deployment" )]
		private static void SelectDeploymentCmd( string type )
		{
			if ( ConsoleSystem.Caller is Player player )
			{
				if ( Game.Instance.Round is HideRound )
					player.Deployment = Enum.Parse<DeploymentType>( type );
			}
		}

		[ConCmd.Client( "hdn_open_deployment", CanBeCalledFromServer = true) ]
		private static void OpenDeploymentCmd( int teamIndex )
		{
			if ( Game.Instance.Round is HideRound round )
			{
				round.OpenDeployment( Game.Instance.GetTeamByIndex( teamIndex ) );
			}
		}

		public static void SelectDeployment( DeploymentType type )
		{
			if ( Local.Pawn is Player player )
				player.Deployment = type;

			SelectDeploymentCmd( type.ToString() );
		}

		public void OpenDeployment( BaseTeam team )
		{
			CloseDeploymentPanel();

			DeploymentPanel = Local.Hud.AddChild<Deployment>();

			team.AddDeployments( DeploymentPanel, (selection) =>
			{
				SelectDeployment( selection );
				CloseDeploymentPanel();
			} );
		}

		public override void OnPlayerSpawn( Player player )
		{
			if ( Players.Contains( player ) ) return;

			AddPlayer( player );

			if ( RoundStarted )
			{
				player.Team = Game.Instance.IrisTeam;
				player.Team.OnStart( player );

				if ( player.Team.HasDeployments )
					OpenDeploymentCmd( To.Single( player ), player.TeamIndex );
			}

			base.OnPlayerSpawn( player );
		}

		protected override void OnStart()
		{
			if ( Host.IsServer )
			{
				foreach ( var client in Client.All )
				{
					if ( client.Pawn is Player player )
						player.Respawn();
				}

				if ( Players.Count == 0 ) return;

				var selectStartIndex = 0;

				if ( !string.IsNullOrEmpty( HostTeam ) && HostTeam == "iris" )
				{
					selectStartIndex = 1;
				}

				// Select a random Hidden player.
				var hidden = Players[Rand.Int( selectStartIndex, Players.Count - 1 )];

				if ( !string.IsNullOrEmpty( HostTeam ) && HostTeam == "hidden" )
				{
					hidden = Players[0];
				}

				Assert.NotNull( hidden );

				hidden.Team = Game.Instance.HiddenTeam;
				hidden.Team.OnStart( hidden );

				// Make everyone else I.R.I.S.
				Players.ForEach( ( player ) =>
				{
					if ( player != hidden )
					{
						player.Team = Game.Instance.IrisTeam;
						player.Team.OnStart( player );
					}

					if ( player.Team.HasDeployments )
						OpenDeploymentCmd( To.Single( player ), player.TeamIndex );
				} );

				RoundStarted = true;
			}
		}

		protected override void OnFinish()
		{
			CloseDeploymentPanel();
		}

		protected override void OnTimeUp()
		{
			Game.Instance.ChangeRound( new HuntRound() );

			base.OnTimeUp();
		}

		private void CloseDeploymentPanel()
		{
			if ( DeploymentPanel != null )
			{
				DeploymentPanel.Delete();
				DeploymentPanel = null;
			}
		}
	}
}
