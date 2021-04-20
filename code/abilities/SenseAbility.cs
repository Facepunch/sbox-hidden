using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public partial class SenseAbility : BaseAbility
	{
		public override float Cooldown => 10;
		public override string Name => "Sense";

		public override string GetKeybind()
		{
			return "G";
		}

		protected override void OnUse( Player player )
		{
			Log.Info( (Host.IsServer ? "Server: " : "Client: ") + "Time Since Last: " + TimeSinceLastUse );

			TimeSinceLastUse = 0;

			using ( Prediction.Off() )
			{
				if ( Host.IsClient )
				{
					_ = StartGlowAbility();
				}
				else
				{
					player.PlaySound( $"i-see-you-{Rand.Int(1, 3)}" );
				}
			}
		}

		public override float GetCooldown( Player player )
		{
			if ( player.Deployment == DeploymentType.HIDDEN_BEAST )
				return Cooldown * 0.5f;
			else if ( player.Deployment == DeploymentType.HIDDEN_ROGUE )
				return Cooldown * 2f;

			return base.GetCooldown( player );
		}

		private async Task StartGlowAbility()
		{
			var players = Game.Instance.GetTeamPlayers<IrisTeam>( true );

			players.ForEach( ( player ) =>
			{
				player.GlowActive = true;
				player.GlowState = GlowStates.GlowStateOn;
				player.GlowDistanceEnd = 1000;
				player.GlowColor = Color.Green;
			} );

			await Task.Delay( TimeSpan.FromSeconds( 3 ) );

			players.ForEach( ( player ) =>
			{
				player.GlowActive = false;
			} );
		}
	}
}

