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

		[NetLocalPredicted] public TimeSince TimeSinceLastUse { get; set; }

		public BaseAbility()
		{
			TimeSinceLastUse = Cooldown;
		}

		public void Use( Player player )
		{
			OnUse( player );

			if ( Host.IsServer )
			{
				using ( Prediction.Off() )
				{
					NetworkDirty( "TimeSinceLastUse", NetVarGroup.NetLocalPredicted );
				}
			}
		}


		public virtual bool IsUsable()
		{
			return TimeSinceLastUse > Cooldown;
		}

		protected virtual void OnUse( Player player ) { }
	}
}

