using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public class HideRound : BaseRound
	{
		public override string RoundName => "Hide / Prepare";
		public override int RoundDuration => 20;

		private bool _roundStarted;

		protected override void OnStart()
		{
			Log.Info( "Started Hide Round" );

			if ( Host.IsServer )
			{
				Sandbox.Player.All.ForEach((player) => player.Respawn());

				if ( Players.Count == 0 ) return;

				// Select a random Hidden player.
				var hidden = Players[Rand.Int( Players.Count - 1 )];

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
				} );

				_roundStarted = true;
			}
		}

		protected override void OnFinish()
		{
			Log.Info( "Finished Hide Round" );
		}

		protected override void OnTimeUp()
		{
			Log.Info( "Hide Time Up!" );

			Game.Instance.ChangeRound( new HuntRound() );

			base.OnTimeUp();
		}

		public override void OnPlayerSpawn( Player player )
		{
			if ( Players.Contains( player ) ) return;

			player.SetModel( "models/citizen/citizen.vmdl" );

			player.EnableAllCollisions = true;
			player.EnableDrawing = true;
			player.EnableHideInFirstPerson = true;
			player.EnableShadowInFirstPerson = true;

			player.ClearAmmo();
			player.Inventory.DeleteContents();

			AddPlayer( player );

			if ( _roundStarted )
			{
				player.Team = Game.Instance.IrisTeam;
				player.Team.OnStart( player );
			}

			base.OnPlayerSpawn( player );
		}
	}
}
