using Sandbox;
using System;

namespace Facepunch.Hidden
{
	public class IrisController : MoveController
	{
		public float FallDamageVelocity { get; set; } = 550f;
		public float FallDamageScale { get; set; } = 0.25f;
		public float MaxSprintSpeed { get; set; } = 300f;
		public float MaxWalkSpeed { get; set; } = 150f;
		public float StaminaLossPerSecond { get; set; } = 15f;
		public float StaminaGainPerSecond { get; set; } = 20f;

		private float FallVelocity { get; set; }

		public override void Simulate()
		{
			Assert.NotNull( Player );

			var staminaLossPerSecond = StaminaLossPerSecond;

			if ( Player.Deployment == DeploymentType.IRIS_BRAWLER )
			{
				staminaLossPerSecond *= 1.3f;

				MaxSprintSpeed = 200f;
				MaxWalkSpeed = 120f;
			}
			else if ( Player.Deployment == DeploymentType.IRIS_TACTICAL )
			{
				MaxSprintSpeed = 250f;
				MaxWalkSpeed = 120f;
			}

			if ( Input.Down( InputButton.Run ) && Player.Velocity.Length >= SprintSpeed * 0.8f )
			{
				Player.StaminaRegenTime = 1f;
				Player.Stamina = MathF.Max( Player.Stamina - (staminaLossPerSecond * Time.Delta), 0f );
			}
			else if ( Player.StaminaRegenTime )
			{
				Player.Stamina = MathF.Min( Player.Stamina + (StaminaGainPerSecond * Time.Delta), 100f );
			}

			SprintSpeed = MaxWalkSpeed + (((MaxSprintSpeed - MaxWalkSpeed) / 100f) * Player.Stamina) + 40f;
			WalkSpeed = MaxWalkSpeed;

			base.Simulate();
		}

		public override void OnPreTickMove()
		{
			FallVelocity = Player.Velocity.z;
		}

		public override void OnPostCategorizePosition( bool stayOnGround, TraceResult trace )
		{
			if ( Host.IsServer && trace.Hit && FallVelocity < -FallDamageVelocity )
			{
				var damage = (MathF.Abs( FallVelocity ) - FallDamageVelocity) * FallDamageScale;

				using ( Prediction.Off() )
				{
					var damageInfo = new DamageInfo()
						.WithAttacker( Player )
						.WithTag( "fall" );

					damageInfo.Damage = damage;

					Player.TakeDamage( damageInfo );
				}
			}
			else if ( trace.Hit && FallVelocity < -(FallDamageVelocity * 0.4f) )
			{
				Player.PlaySound( "soft.impact" );
			}
		}
	}
}
