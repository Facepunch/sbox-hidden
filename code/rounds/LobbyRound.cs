using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
    public class LobbyRound : BaseRound
	{
		public override string RoundName => "Lobby";

		protected override void OnStart()
		{
			Log.Info( "Started Lobby Round" );

			if ( Host.IsServer )
			{
				Sandbox.Player.All.ForEach( ( player ) => (player as Player).Respawn() );
			}
		}

		protected override void OnFinish()
		{
			Log.Info( "Finished Lobby Round" );
		}

		public override void OnPlayerKilled( Player player )
		{
			player.Respawn();

			base.OnPlayerKilled( player );
		}

		public override void OnPlayerSpawn( Player player )
		{
			if ( Players.Contains( player ) )
			{
				player.Team.SupplyLoadout( player );
				return;
			}

			AddPlayer( player );

			player.Team = Game.Instance.IrisTeam;
			player.Team.OnStart( player );
			player.Team.SupplyLoadout( player );

			base.OnPlayerSpawn( player );
		}
	}
}
