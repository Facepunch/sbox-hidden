using Sandbox;
using System;

namespace Facepunch.Hidden
{
	public abstract class BaseTeam
	{
		public int Index { get; internal set; }

		public virtual Color Color => Color.White;
		public virtual bool HasDeployments => true;
		public virtual bool HideNameplate => false;
		public virtual string HudClassName => "";
		public virtual string Name => "";

		public void Join( HiddenPlayer player )
		{
			if ( player.IsLocalPawn )
			{
				InputHints.UpdateOnClient();
			}

			OnJoin( player );
		}

		public void Leave( HiddenPlayer player )
		{
			OnLeave( player );
		}

		public virtual void OnTick() { }

		public virtual void Simulate( HiddenPlayer player ) { }

		public virtual void OnLeave( HiddenPlayer player  ) { }

		public virtual void OnJoin( HiddenPlayer player  ) { }

		public virtual void OnStart( HiddenPlayer player ) { }

		public virtual void OnTakeDamageFromPlayer( HiddenPlayer player, HiddenPlayer attacker, DamageInfo info ) { }

		public virtual void OnDealDamageToPlayer( HiddenPlayer player, HiddenPlayer target, DamageInfo info ) { }

		public virtual void AddDeployments( Deployment panel, Action<DeploymentType> callback ) { }

		public virtual void OnPlayerKilled( HiddenPlayer player ) { }

		public virtual void SupplyLoadout( HiddenPlayer player  ) { }

		public virtual bool PlayPainSounds( HiddenPlayer player )
		{
			return false;
		}
	}
}
