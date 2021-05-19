using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public partial class HideRound : BaseRound
	{
		[ServerVar( "hdn_host_always_hidden", Help = "Make the host always the hidden." )]
		public static bool HostAlwaysHidden { get; set; } = false;

		public override string RoundName => "PREPARE";
		public override int RoundDuration => 20;

		private Deployment _deploymentPanel;
		private bool _roundStarted;

		[ServerCmd( "hdn_select_deployment" )]
		private static void SelectDeploymentCmd( string type )
		{
			if ( ConsoleSystem.Caller is Player player )
			{
				if ( Game.Instance.Round is HideRound )
					player.Deployment = Enum.Parse<DeploymentType>( type );
			}
		}

		[ClientCmd( "hdn_open_deployment", CanBeCalledFromServer = true) ]
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

			_deploymentPanel = Local.Hud.AddChild<Deployment>();

			team.AddDeployments( _deploymentPanel, (selection) =>
			{
				SelectDeployment( selection );
				CloseDeploymentPanel();
			} );
		}

		public override void OnPlayerSpawn( Player player )
		{
			if ( Players.Contains( player ) ) return;

			AddPlayer( player );

			if ( _roundStarted )
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
			Log.Info( "Started Hide Round" );

			if ( Host.IsServer )
			{
				foreach ( var client in Client.All )
				{
					if ( client.Pawn is Player player )
						player.Respawn();
				}

				if ( Players.Count == 0 ) return;

				// Select a random Hidden player.
				var hidden = Players[Rand.Int( Players.Count - 1 )];

				if ( HostAlwaysHidden )
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

				_roundStarted = true;
			}
		}

		protected override void OnFinish()
		{
			Log.Info( "Finished Hide Round" );

			CloseDeploymentPanel();
		}

		protected override void OnTimeUp()
		{
			Log.Info( "Hide Time Up!" );

			Game.Instance.ChangeRound( new HuntRound() );

			base.OnTimeUp();
		}

		private void CloseDeploymentPanel()
		{
			if ( _deploymentPanel != null )
			{
				_deploymentPanel.Delete();
				_deploymentPanel = null;
			}
		}
	}
}
