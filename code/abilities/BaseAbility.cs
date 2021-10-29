using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
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

		public void Use( Player player )
		{
			OnUse( player );
		}

		public float GetCooldownTimeLeft( Player player )
		{
			if ( TimeSinceLastUse == -1 )
				return 0;

			return GetCooldown( player ) - TimeSinceLastUse;
		}

		public virtual float GetCooldown( Player player )
		{
			return Cooldown;
		}


		public virtual string GetKeybind()
		{
			return "";
		}

		public virtual bool IsUsable( Player player )
		{
			return ( TimeSinceLastUse == -1 || TimeSinceLastUse > GetCooldown( player ) );
		}

		protected virtual void OnUse( Player player ) { }
	}
}

