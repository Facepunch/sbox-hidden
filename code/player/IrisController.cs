using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public class IrisController : WalkController
	{
		public float MaxSprintSpeed = 300f;

		public override void Tick()
		{
			if ( Player is Player player )
			{
				if ( Input.Down( InputButton.Run ) && Velocity.Length >= (SprintSpeed * 0.8f) )
				{
					player.Stamina = MathF.Max( player.Stamina - (15f * Time.Delta), 0f );
				}
				else
				{
					player.Stamina = MathF.Min( player.Stamina + (10f * Time.Delta), 100f );
				}

				SprintSpeed = WalkSpeed + (((MaxSprintSpeed - WalkSpeed) / 100f) * player.Stamina) + 40f;
			}

			base.Tick();
		}
	}
}
