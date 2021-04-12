using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public class HiddenController : WalkController
	{
		public bool IsFrozen { get; set; }
		public bool IsSliding { get; set; }
		public float SlideVelocity { get; set; }

		public override void Tick()
		{
			if ( IsFrozen )
			{
				// We've stuck to a wall, do not simulate movement.
				return;
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
