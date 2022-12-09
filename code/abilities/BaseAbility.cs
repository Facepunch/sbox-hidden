using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
	public partial class BaseAbility : BaseNetworkable
	{
		public virtual float Cooldown => 1;
		public virtual string Name => "";

		[Net, Local, Predicted] public TimeSince TimeSinceLastUse { get; set; }

		public BaseAbility()
		{
			TimeSinceLastUse = -1;
		}

		public void Use( HiddenPlayer player )
		{
			OnUse( player );
		}

		public float GetCooldownTimeLeft( HiddenPlayer player )
		{
			if ( TimeSinceLastUse == -1 )
				return 0;

			return GetCooldown( player ) - TimeSinceLastUse;
		}

		public virtual float GetCooldown( HiddenPlayer player )
		{
			return Cooldown;
		}


		public virtual string GetKeybind()
		{
			return "";
		}

		public virtual bool IsUsable( HiddenPlayer player )
		{
			return ( TimeSinceLastUse == -1 || TimeSinceLastUse > GetCooldown( player ) );
		}

		protected virtual void OnUse( HiddenPlayer player ) { }
	}
}

