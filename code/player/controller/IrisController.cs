using Sandbox;
using System;

namespace Facepunch.Hidden
{
	public class IrisController : CustomWalkController
	{
		public float FallDamageVelocity = 550f;
		public float FallDamageScale = 0.25f;
		public float MaxSprintSpeed = 300f;
		public float MaxWalkSpeed = 150f;
		public float StaminaLossPerSecond = 15f;
		public float StaminaGainPerSecond = 20f;

		private float FallVelocity;

		public override void Simulate()
		{
			if ( Pawn is Player player )
			{
				var staminaLossPerSecond = StaminaLossPerSecond;

				if ( player.Deployment == DeploymentType.IRIS_BRAWLER )
				{
					staminaLossPerSecond *= 1.3f;

					MaxSprintSpeed = 200f;
					MaxWalkSpeed = 120f;
				}
				else if ( player.Deployment == DeploymentType.IRIS_TACTICAL )
				{
					MaxSprintSpeed = 250f;
					MaxWalkSpeed = 120f;
				}

				if ( Input.Down( InputButton.Run ) && Velocity.Length >= SprintSpeed * 0.8f )
					player.Stamina = MathF.Max( player.Stamina - (staminaLossPerSecond * Time.Delta), 0f );
				else
					player.Stamina = MathF.Min( player.Stamina + (StaminaGainPerSecond * Time.Delta), 100f );

				SprintSpeed = MaxWalkSpeed + (((MaxSprintSpeed - MaxWalkSpeed) / 100f) * player.Stamina) + 40f;
				WalkSpeed = MaxWalkSpeed;
			}

			base.Simulate();
		}

		public override void OnPreTickMove()
		{
			FallVelocity = Velocity.z;
		}

		public override void OnPostCategorizePosition( bool stayOnGround, TraceResult trace )
		{
			if ( Host.IsServer && trace.Hit && FallVelocity < -FallDamageVelocity )
			{
				var damage = (MathF.Abs( FallVelocity ) - FallDamageVelocity) * FallDamageScale;

				using ( Prediction.Off() )
				{
					var damageInfo = new DamageInfo()
						.WithAttacker( Pawn )
						.WithFlag( DamageFlags.Fall );

					damageInfo.Damage = damage;

					Pawn.TakeDamage( damageInfo );
				}
			}
		}
	}
}
