using Sandbox;
using Sandbox.Diagnostics;
using System;

namespace Facepunch.Hidden
{
	public partial class HiddenController : MoveController
	{
		public override bool EnableSprinting => false;
		public override float WalkSpeed { get; set; } = 380f;

		public float LeapVelocity { get; set; } = 300f;
		public float LeapStaminaLoss { get; set; } = 25f;

		private float FallVelocity { get; set; }

		public virtual void CheckLeapButton()
		{
			if ( Swimming )
			{
				ClearGroundEntity();
				Player.Velocity = Player.Velocity.WithZ( 100f );
				return;
			}

			if ( !Player.GroundEntity.IsValid() )
				return;

			ClearGroundEntity();

			var flGroundFactor = 1f;
			var flMul = 268.3281572999747f * 1.2f;
			var startZ = Player.Velocity.z;

			Player.Velocity = Player.Velocity.WithZ( startZ + flMul * flGroundFactor );
			Player.Velocity -= new Vector3( 0f, 0f, Gravity * 0.5f ) * Time.Delta;

			if ( Player.Stamina > 20f )
			{
				var minLeapVelocity = (LeapVelocity * 0.2f);
				var extraLeapVelocity = (LeapVelocity * 0.8f);
				var actualLeapVelocity = minLeapVelocity + (extraLeapVelocity / 100f) * Player.Stamina;
				var rotation = Player.ViewAngles.ToRotation();

				Player.Velocity += (rotation.Forward * actualLeapVelocity);

				Player.PlaySound( "hidden.leap" );

				Player.TimeSinceLastLeap = 0f;
				Player.StaminaRegenTime = 1f;
				Player.Stamina = MathF.Max( Player.Stamina - LeapStaminaLoss, 0f );
			}

			AddEvent( "jump" );
		}

		public override void HandleJumping()
		{
			if ( AutoJump ? Input.Down( InputButton.Jump ) : Input.Pressed( InputButton.Jump ) )
				CheckJumpButton();
			else if ( Input.Pressed( InputButton.Run ) )
				CheckLeapButton();
		}

		public override float GetWishSpeed()
		{
			var speed = base.GetWishSpeed();

			if ( Player.Deployment == DeploymentType.HIDDEN_BEAST )
				speed *= 0.75f;
			else if ( Player.Deployment == DeploymentType.HIDDEN_ROGUE )
				speed *= 1.25f;

			return speed;
		}

		public override void Simulate()
		{
			Assert.NotNull( Player );

			if ( Player.IsFrozen )
			{
				if ( Input.Released( InputButton.Run ) || Player.Stamina <= 5 )
				{
					Player.TimeSinceLastLeap = 0f;

					var rotation = Player.ViewAngles.ToRotation();

					Player.BaseVelocity = Vector3.Zero;
					WishVelocity = Vector3.Zero;
					Player.Velocity = (rotation.Forward * LeapVelocity * 2f);
					Player.IsFrozen = false;
				}

				Player.Stamina = MathF.Max( Player.Stamina - (5f * Time.Delta), 0f );

				return;
			}

			if ( Player.StaminaRegenTime )
			{
				Player.Stamina = MathF.Min( Player.Stamina + (10f * Time.Delta), 100f );
			}

			base.Simulate();
		}

		public override void OnPreTickMove()
		{
			FallVelocity = Player.Velocity.z;
		}

		public override void OnPostCategorizePosition( bool stayOnGround, TraceResult trace )
		{
			if ( trace.Hit && FallVelocity < -200f )
			{
				Player.PlaySound( "soft.impact" );
			}
		}
	}
}
