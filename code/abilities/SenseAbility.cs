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
		public override float Cooldown => 20;
		public override string Name => "Sense";

		public override void OnUse( Player player )
		{
			Log.Info( (Host.IsServer ? "Server: " : "Client: ") + "Time Since Last: " + TimeSinceLastUse );

			TimeSinceLastUse = 0;

			if ( Host.IsServer )
			{
				using ( Prediction.Off() )
				{
					_ = StartGlowAbility();
				}

				NetworkDirty( "TimeSinceLastUse", NetVarGroup.NetLocalPredicted );
			}
		}

		private async Task StartGlowAbility()
		{
			Game.IrisTeam.Players.ForEach( ( player ) =>
			{
				player.GlowActive = true;
				player.GlowState = GlowStates.GlowStateOn;
				player.GlowDistanceEnd = 1000;
				player.GlowColor = Color.Green;
			} );

			await Task.Delay( TimeSpan.FromSeconds( 5 ) );

			Game.IrisTeam.Players.ForEach( ( player ) =>
			{
				player.GlowActive = false;
			} );
		}
	}
}

