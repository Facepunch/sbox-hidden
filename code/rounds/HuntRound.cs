using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
    public class HuntRound : BaseRound
	{
		public override string RoundName => "Hunt / Survive";
		public override int RoundDuration => 300;

		public List<Player> Spectators = new();

		protected override void OnStart()
		{
			Log.Info( "Started Hunt Round" );

			if ( Host.IsServer )
			{
				Sandbox.Player.All.ForEach( ( player ) => OnPlayerStart( player as Player ) );
			}
		}

		protected override void OnFinish()
		{
			Log.Info( "Finished Hunt Round" );

			if ( Host.IsServer )
			{
				Spectators.Clear();
			}
		}

		protected override void OnTimeUp()
		{
			Log.Info( "Hunt Time Up!" );

			Game.Instance.ChangeRound( new StatsRound() );

			base.OnTimeUp();
		}

		protected void OnPlayerStart(Player player)
		{
			// Give everyone their starting loadouts.
			player.Team?.SupplyLoadout( player );
		}

		public override void OnPlayerKilled( Player player )
		{
			Players.Remove( player );
			Spectators.Add( player );

			player.Hide();

			if ( player.Team is HiddenTeam )
			{
				// TODO: The Hidden is dead! Mission accomplished!
				Game.Instance.ChangeRound( new StatsRound() );

				return;
			}

			if ( Players.Count <= 1 )
			{
				// The Hidden has won the game!
				Game.Instance.ChangeRound( new StatsRound() );
			}
		}

		public override void OnPlayerLeave( Player player )
		{
			base.OnPlayerLeave( player );

			Spectators.Remove( player );

			if ( player.Team is HiddenTeam )
			{
				// TODO: The Hidden left the game... how frustrating for everybody (pick a random person to take over?)
				Game.Instance.ChangeRound( new StatsRound() );
			}
		}

		public override void OnPlayerSpawn( Player player )
		{
			player.SetModel( "models/citizen/citizen.vmdl" );
			player.Hide();

			Spectators.Add( player );
			Players.Remove( player );

			Log.Info( player.Name + " spawned in, hiding them. " );

			base.OnPlayerSpawn( player );
		}
	}
}
