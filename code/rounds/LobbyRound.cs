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
		public override string RoundName => "Not Enough Players...";

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

		public override void OnPlayerSpawn( Player player )
		{
			if ( Players.Contains( player ) ) return;

			player.SetModel( "models/citizen/citizen.vmdl" );
			player.Hide();

			AddPlayer( player );

			base.OnPlayerSpawn( player );
		}
	}
}
