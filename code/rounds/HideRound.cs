using Sandbox;
using Sandbox.Diagnostics;
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
		public static void SelectDeploymentCmd( string type )
		{
			if ( ConsoleSystem.Caller.Pawn is HiddenPlayer player )
			{
				if ( HiddenGame.Entity.Round is HideRound )
				{
					player.Deployment = Enum.Parse<DeploymentType>( type );
				}
			}
		}

		[ConCmd.Client( "hdn_open_deployment", CanBeCalledFromServer = true) ]
		public static void OpenDeploymentCmd( int teamIndex )
		{
			if ( HiddenGame.Entity.Round is HideRound round )
			{
				round.OpenDeployment( HiddenGame.Entity.GetTeamByIndex( teamIndex ) );
			}
		}

		public static void SelectDeployment( DeploymentType type )
		{
			if ( Game.LocalPawn is HiddenPlayer player )
				player.Deployment = type;

			SelectDeploymentCmd( type.ToString() );
		}

		public void OpenDeployment( BaseTeam team )
		{
			CloseDeploymentPanel();

			DeploymentPanel = Game.RootPanel.AddChild<Deployment>();

			team.AddDeployments( DeploymentPanel, (selection) =>
			{
				SelectDeployment( selection );
				CloseDeploymentPanel();
			} );
		}

		public override void OnPlayerSpawn( HiddenPlayer player )
		{
			if ( Players.Contains( player ) ) return;

			AddPlayer( player );

			if ( RoundStarted )
			{
				player.Team = HiddenGame.Entity.IrisTeam;
				player.Team.OnStart( player );

				if ( player.Team.HasDeployments )
					OpenDeploymentCmd( To.Single( player ), player.TeamIndex );
			}

			base.OnPlayerSpawn( player );
		}

		protected override void OnStart()
		{
			if ( Game.IsServer )
			{
				HiddenPlayer.ClearAllBloodParticles();

				foreach ( var client in Game.Clients )
				{
					if ( client.Pawn is HiddenPlayer player )
						player.Respawn();
				}

				if ( Players.Count == 0 ) return;

				var selectStartIndex = 0;

				if ( !string.IsNullOrEmpty( HostTeam ) && HostTeam == "iris" )
				{
					selectStartIndex = 1;
				}

				// Select a random Hidden player.
				var hidden = Players[Game.Random.Int( selectStartIndex, Players.Count - 1 )];

				if ( !string.IsNullOrEmpty( HostTeam ) && HostTeam == "hidden" )
				{
					hidden = Players[0];
				}

				Assert.NotNull( hidden );

				hidden.Team = HiddenGame.Entity.HiddenTeam;
				hidden.Team.OnStart( hidden );

				// Make everyone else I.R.I.S.
				Players.ForEach( ( player ) =>
				{
					if ( player != hidden )
					{
						player.Team = HiddenGame.Entity.IrisTeam;
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
			HiddenGame.Entity.ChangeRound( new HuntRound() );

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
