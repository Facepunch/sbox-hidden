using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
	public partial class SenseAbility : BaseAbility
	{
		public override float Cooldown => 10;
		public override string Name => "Sense";

		public override string GetKeybind()
		{
			return Input.GetKeyWithBinding( "iv_drop" ).ToUpper();
		}

		protected override void OnUse( Player player )
		{
			TimeSinceLastUse = 0;

			using ( Prediction.Off() )
			{
				if ( Host.IsClient )
				{
					_ = StartGlowAbility( player );
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

		private async Task StartGlowAbility( Player caller )
		{
			var players = Game.Instance.GetTeamPlayers<IrisTeam>( true );

			caller.IsSenseActive = true;

			players.ForEach( ( player ) =>
			{
				player.ShowSenseParticles( true );
			} );

			await Task.Delay( TimeSpan.FromSeconds( Cooldown * 0.5f ) );

			players.ForEach( ( player ) =>
			{
				player.ShowSenseParticles( false );
			} );

			caller.IsSenseActive = false;
		}
	}
}

