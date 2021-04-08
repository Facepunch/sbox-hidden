using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public partial class BaseAbility : NetworkClass
	{
		public virtual float Cooldown => 1;
		public virtual string Name => "";

		[NetLocal] public TimeSince TimeSinceLastUse { get; set; }

		public BaseAbility()
		{
			TimeSinceLastUse = Cooldown;
		}

		public virtual bool IsUsable()
		{
			return TimeSinceLastUse > Cooldown;
		}

		public virtual void OnUse( Player player ) { }
	}
}

