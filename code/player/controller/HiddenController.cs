using Sandbox;
using System;

namespace Facepunch.Hidden
{
	public partial class HiddenController : CustomWalkController
	{
		[Net, Predicted] public bool IsFrozen { get; set; }

		public override bool EnableSprinting => false;
		public override float WalkSpeed { get; set; } = 380f;

		public float LeapVelocity { get; set; } = 300f;
		public float LeapStaminaLoss { get; set; } = 25f;

		private float FallVelocity;

		public virtual void CheckLeapButton()
		{
			if ( Swimming )
			{
				ClearGroundEntity();
				Velocity = Velocity.WithZ( 100f );
				return;
			}

			if ( GroundEntity == null )
				return;

			ClearGroundEntity();

			var flGroundFactor = 1f;
			var flMul = 268.3281572999747f * 1.2f;
			var startZ = Velocity.z;

			Velocity = Velocity.WithZ( startZ + flMul * flGroundFactor );
			Velocity -= new Vector3( 0f, 0f, Gravity * 0.5f ) * Time.Delta;

			if ( Pawn is Player player && player.Stamina > 20f )
			{
				var minLeapVelocity = (LeapVelocity * 0.2f);
				var extraLeapVelocity = (LeapVelocity * 0.8f);
				var actualLeapVelocity = minLeapVelocity + (extraLeapVelocity / 100f) * player.Stamina;

				Velocity += (Input.Rotation.Forward * actualLeapVelocity);

				player.PlaySound( "hidden.leap" );

				player.TimeSinceLastLeap = 0f;
				player.StaminaRegenTime = 1f;
				player.Stamina = MathF.Max( player.Stamina - LeapStaminaLoss, 0f );
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

			if ( Pawn is Player player )
			{
				if ( player.Deployment == DeploymentType.HIDDEN_BEAST )
					speed *= 0.75f;
				else if ( player.Deployment == DeploymentType.HIDDEN_ROGUE )
					speed *= 1.25f;
			}

			return speed;
		}

		public override void Simulate()
		{
			if ( Pawn is not Player player ) return;

			if ( IsFrozen )
			{
				if ( Input.Released( InputButton.Run ) || player.Stamina <= 5 )
				{
					player.TimeSinceLastLeap = 0f;

					BaseVelocity = Vector3.Zero;
					WishVelocity = Vector3.Zero;
					Velocity = (Input.Rotation.Forward * LeapVelocity * 2f);
					IsFrozen = false;
				}

				player.Stamina = MathF.Max( player.Stamina - (5f * Time.Delta), 0f );

				return;
			}

			if ( player.StaminaRegenTime )
			{
				player.Stamina = MathF.Min( player.Stamina + (10f * Time.Delta), 100f );
			}

			base.Simulate();
		}

		public override void OnPreTickMove()
		{
			FallVelocity = Velocity.z;
		}

		public override void OnPostCategorizePosition( bool stayOnGround, TraceResult trace )
		{
			if ( trace.Hit && FallVelocity < -200f )
			{
				Pawn.PlaySound( "soft.impact" );
			}
		}
	}
}
