using Sandbox;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
    public class LobbyRound : BaseRound
	{
		public override string RoundName => "LOBBY";
		public override bool CanPlayerTakeDamage => false;

		protected override void OnStart()
		{
			if ( Game.IsServer )
			{
				foreach ( var client in Game.Clients )
				{
					if ( client.Pawn is HiddenPlayer player )
						player.Respawn();
				}
			}
		}

		protected override void OnFinish()
		{

		}

		public override void OnPlayerKilled( HiddenPlayer player )
		{
			_ = StartRespawnTimer( player );

			base.OnPlayerKilled( player );
		}

		private async Task StartRespawnTimer( HiddenPlayer player )
		{
			await Task.Delay( 1000 );

			player.Respawn();
		}

		public override void OnPlayerSpawn( HiddenPlayer player )
		{
			if ( Players.Contains( player ) )
			{
				player.Team.SupplyLoadout( player );
				return;
			}

			AddPlayer( player );

			player.Team = HiddenGame.Entity.IrisTeam;
			player.Team.OnStart( player );
			player.Team.SupplyLoadout( player );

			base.OnPlayerSpawn( player );
		}
	}
}
