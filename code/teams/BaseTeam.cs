using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public abstract class BaseTeam : Entity
	{
		public List<Player> Players { get; set; } = new();

		public virtual bool HideNameplate => false;
		public virtual string HudClassName => "";

		public BaseTeam()
		{
			Transmit = TransmitType.Always;
		}

		public void Join( Player player )
		{
			if ( player.IsLocalPlayer )
			{
				Log.Info( "Adding " + HudClassName + " to the HUD" );

				Sandbox.Hud.CurrentPanel.AddClass( HudClassName );
			}

			OnJoin( player );
		}

		public void Leave( Player player )
		{
			if ( player.IsLocalPlayer )
			{
				Sandbox.Hud.CurrentPanel.RemoveClass( HudClassName );
			}

			OnLeave( player );
		}

		public virtual void OnTick( Player player ) { }

		public virtual void OnLeave( Player player  ) { }

		public virtual void OnJoin( Player player  ) { }

		public virtual void OnPlayerKilled( Player player ) { }

		public virtual void SupplyLoadout( Player player  ) { }
	}
}
