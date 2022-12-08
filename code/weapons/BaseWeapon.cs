using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	public abstract partial class BaseWeapon : AnimatedEntity
	{
		public virtual string ViewModelPath => null;
		public BaseViewModel ViewModelEntity { get; protected set; }

		public virtual ModelEntity EffectEntity => (ViewModelEntity.IsValid() && IsFirstPersonMode) ? ViewModelEntity : this;

		public virtual float PrimaryRate => 5.0f;
		public virtual float SecondaryRate => 15.0f;

		[Net, Predicted]
		public TimeSince TimeSincePrimaryAttack { get; set; }

		[Net, Predicted]
		public TimeSince TimeSinceSecondaryAttack { get; set; }

		public override void Simulate( Client player )
		{
			if ( CanReload() )
			{
				Reload();
			}

			if ( !Owner.IsValid() )
				return;

			if ( CanPrimaryAttack() )
			{
				using ( LagCompensation() )
				{
					TimeSincePrimaryAttack = 0;
					AttackPrimary();
				}
			}

			if ( !Owner.IsValid() )
				return;

			if ( CanSecondaryAttack() )
			{
				using ( LagCompensation() )
				{
					TimeSinceSecondaryAttack = 0;
					AttackSecondary();
				}
			}
		}

		public virtual bool CanReload()
		{
			if ( !Owner.IsValid() || !Input.Down( InputButton.Reload ) ) return false;

			return true;
		}

		public virtual void Reload()
		{

		}

		public virtual bool CanPrimaryAttack()
		{
			if ( !Owner.IsValid() || !Input.Down( InputButton.PrimaryAttack ) ) return false;

			var rate = PrimaryRate;
			if ( rate <= 0 ) return true;

			return TimeSincePrimaryAttack > (1 / rate);
		}

		public virtual void AttackPrimary()
		{

		}

		public virtual bool CanSecondaryAttack()
		{
			if ( !Owner.IsValid() || !Input.Down( InputButton.SecondaryAttack ) ) return false;

			var rate = SecondaryRate;
			if ( rate <= 0 ) return true;

			return TimeSinceSecondaryAttack > (1 / rate);
		}

		public virtual void AttackSecondary()
		{

		}

		public virtual IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
		{
			bool underWater = Trace.TestPoint( start, "water" );

			var trace = Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc" )
				.Ignore( this )
				.Size( radius );

			if ( !underWater )
				trace = trace.WithAnyTags( "water" );

			var result = trace.Run();

			if ( result.Hit )
				yield return result;
		}

		public virtual bool CanCarry( Entity carrier )
		{
			return true;
		}

		public virtual void OnCarryStart( Entity carrier )
		{
			if ( IsClient ) return;

			SetParent( carrier, true );
			Owner = carrier;
			EnableAllCollisions = false;
			EnableDrawing = false;
		}

		public virtual void SimulateAnimator( AnimationHelperWithLegs anim )
		{
			anim.HoldType = AnimationHelperWithLegs.HoldTypes.Pistol;
			anim.Handedness = AnimationHelperWithLegs.Hand.Both;
			anim.AimBodyWeight = 1f;
		}

		public virtual void OnCarryDrop( Entity dropper )
		{
			if ( IsClient ) return;

			SetParent( null );
			Owner = null;
			EnableDrawing = true;
			EnableAllCollisions = true;
		}

		public virtual void ActiveStart( Entity ent )
		{
			EnableDrawing = true;

			if ( IsLocalPawn )
			{
				DestroyViewModel();
				CreateViewModel();
			}
		}

		public virtual void ActiveEnd( Entity ent, bool dropped )
		{
			if ( !dropped )
			{
				EnableDrawing = false;
			}

			if ( IsClient )
			{
				DestroyViewModel();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if ( IsClient && ViewModelEntity.IsValid() )
			{
				DestroyViewModel();
			}
		}

		public virtual void CreateViewModel()
		{
			Host.AssertClient();

			if ( string.IsNullOrEmpty( ViewModelPath ) )
				return;

			ViewModelEntity = new BaseViewModel();
			ViewModelEntity.Position = Position;
			ViewModelEntity.Owner = Owner;
			ViewModelEntity.EnableViewmodelRendering = true;
			ViewModelEntity.SetModel( ViewModelPath );
		}

		public virtual void DestroyViewModel()
		{
			ViewModelEntity?.Delete();
			ViewModelEntity = null;
		}

		public override Sound PlaySound( string soundName, string attachment )
		{
			if ( Owner.IsValid() )
				return Owner.PlaySound( soundName, attachment );

			return base.PlaySound( soundName, attachment );
		}

		public override void Spawn()
		{
			base.Spawn();

			PhysicsEnabled = true;
			UsePhysicsCollision = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;
		}
	}
}
