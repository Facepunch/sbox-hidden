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
		public override string RoundName => "LOBBY";

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
			_ = StartRespawnTimer( player );

			base.OnPlayerKilled( player );
		}

		private async Task StartRespawnTimer( Player player )
		{
			await Task.Delay( 1000 );

			player.Respawn();
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
