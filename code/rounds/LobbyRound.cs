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
			if ( Host.IsServer )
			{
				foreach ( var client in Client.All )
				{
					if ( client.Pawn is Player player )
						player.Respawn();
				}
			}
		}

		protected override void OnFinish()
		{

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
