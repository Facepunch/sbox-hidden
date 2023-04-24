﻿using Sandbox;
using System;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
	public partial class SenseAbility : BaseAbility
	{
		public override float Cooldown => 5;
		public override string Name => "Sense";

		public override string GetKeybind()
		{
			return Input.GetButtonOrigin( "drop" ).ToUpper();
		}

		protected override void OnUse( HiddenPlayer player )
		{
			TimeSinceLastUse = 0;

			using ( Prediction.Off() )
			{
				if ( Game.IsClient )
				{
					_ = StartGlowAbility( player );
				}
				else
				{
					player.PlaySound( $"i-see-you-{Game.Random.Int(1, 3)}" );
				}
			}
		}

		public override float GetCooldown( HiddenPlayer player )
		{
			if ( player.Deployment == DeploymentType.HIDDEN_BEAST )
				return Cooldown * 0.5f;
			else if ( player.Deployment == DeploymentType.HIDDEN_ROGUE )
				return Cooldown * 2f;

			return base.GetCooldown( player );
		}

		private async Task StartGlowAbility( HiddenPlayer caller )
		{
			var players = HiddenGame.Entity.GetTeamPlayers<IrisTeam>( true );

			caller.IsSenseActive = true;

			foreach ( var player in players )
			{
				player.ShowSenseParticles( true );
			}

			await Task.Delay( TimeSpan.FromSeconds( Cooldown * 0.5f ) );

			foreach ( var player in players )
			{
				player.ShowSenseParticles( false );
			}

			caller.IsSenseActive = false;
		}
	}
}

