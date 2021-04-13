using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public class HiddenController : CustomWalkController
	{
		public override float SprintSpeed { get; set; } = 380f;
		public bool IsFrozen { get; set; }
		public bool IsSliding { get; set; }
		public float SlideVelocity { get; set; }
		public float LeapVelocity { get; set; } = 300f;
		public float LeapStaminaLoss { get; set; } = 40f;

		protected override void AddJumpVelocity()
		{
			if ( Player is Player player )
			{
				var minLeapVelocity = (LeapVelocity * 0.2f);
				var extraLeapVelocity = (LeapVelocity * 0.8f);
				var actualLeapVelocity = minLeapVelocity + ( extraLeapVelocity / 100f) * player.Stamina;

				Velocity += (Player.EyeRot.Forward * actualLeapVelocity);

				player.Stamina = MathF.Max( player.Stamina - LeapStaminaLoss, 0f );
			}

			base.AddJumpVelocity();
		}

		public override void Tick()
		{
			if ( IsFrozen )
			{
				if ( Input.Pressed( InputButton.Jump ) )
				{
					BaseVelocity = Vector3.Zero;
					WishVelocity = Vector3.Zero;
					Velocity = ( Player.EyeRot.Forward * LeapVelocity * 2f);
					IsFrozen = false;
				}

				return;
			}

			if ( Player is Player player )
			{
				player.Stamina = MathF.Min( player.Stamina + (10f * Time.Delta), 100f );
			}

			// TODO: We'll implement jump stamina here. We can't do it yet.

			/*
			if ( !IsSliding && Input.Pressed( InputButton.Duck) )
			{
				if ( GroundEntity != null && Velocity.Length >= SprintSpeed * 0.8f )
				{
					SlideVelocity = 2000f;
					Velocity = Rot.Forward * 1000f;
					IsSliding = true;
				}
			}
			else if ( IsSliding )
			{
				if ( GroundEntity == null || !Input.Down( InputButton.Duck ) )
				{
					IsSliding = false;
				}
				else
				{
					SlideVelocity *= 0.98f;
					Velocity += (Rot.Forward * SlideVelocity * Time.Delta);
				}
			}
			*/

			base.Tick();
		}
	}
}
