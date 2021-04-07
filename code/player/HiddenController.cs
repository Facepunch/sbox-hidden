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

		public override void Tick()
		{
			if ( IsFrozen )
			{
				// We've stuck to a wall, do not simulate movement.
				return;
			}

			base.Tick();
		}
	}
}
