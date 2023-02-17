using Sandbox;

namespace Facepunch.Hidden
{
	public abstract partial class Throwable<T> : ProjectileWeapon<T> where T : Projectile, new()
	{
		public override string ImpactEffect => null;
		public override string ViewModelPath => "models/grenade/fp_grenade.vmdl";
		public override int ViewModelMaterialGroup => 1;
		public override string MuzzleFlashEffect => null;
		public override float PrimaryRate => 1f;
		public override float SecondaryRate => 1f;
		public override float InheritVelocity => 0f;
		public override int ClipSize => 0;
		public override float ReloadTime => 2.3f;

		public virtual float ThrowAnimationTime => 0.8f;
		public virtual string ThrowSound => null;

		[Net, Predicted] private TimeUntil NextThrowTime { get; set; }
		[Net, Predicted] private bool HasBeenThrown { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/weapons/w_held_item.vmdl" );
		}

		public override void ActiveEnd( Entity owner, bool dropped )
		{
			if ( HasBeenThrown )
			{
				HasBeenThrown = false;
				AmmoClip++;
			}

			base.ActiveEnd( owner, dropped );
		}

		public override void AttackPrimary()
		{
			if ( HasBeenThrown ) return;

			if ( !TakeAmmo( 1 ) ) return;

			if ( !string.IsNullOrEmpty( ThrowSound ) )
				PlaySound( ThrowSound );

			PlayAttackAnimation();
			ShootEffects();
			OnThrown();

			NextThrowTime = ThrowAnimationTime;
			HasBeenThrown = true;
		}

		public override void Simulate( IClient owner )
		{
			if ( Prediction.FirstTime && HasBeenThrown && NextThrowTime )
			{
				if ( AmmoClip > 0 )
				{
					ViewModelEntity?.SetAnimParameter( "deploy", true );
				}

				Game.SetRandomSeed( Time.Tick );
				FireProjectile();

				HasBeenThrown = false;
			}

			base.Simulate( owner );
		}

		public override void CreateViewModel()
		{
			if ( AmmoClip > 0 )
			{
				base.CreateViewModel();
				ViewModelEntity?.SetAnimParameter( "deploy", true );
			}
		}

		public override void SimulateAnimator( AnimationHelperWithLegs anim )
		{
			anim.HoldType = AnimationHelperWithLegs.HoldTypes.Punch;
		}

		protected virtual void OnThrown()
		{

		}

		protected override void ShootEffects()
		{
			base.ShootEffects();

			ViewModelEntity?.SetAnimParameter( "attack", true );
			ViewModelEntity?.SetAnimParameter( "holdtype_attack", 1 );
		}

		protected override void OnProjectileFired( T projectile )
		{
			if ( Game.IsClient && IsFirstPersonMode )
			{
				//projectile.Position = EffectEntity.Position + EffectEntity.Rotation.Forward * 24f + EffectEntity.Rotation.Right * 8f + EffectEntity.Rotation.Down * 4f;
			}
		}

		protected override void OnProjectileHit( T projectile, TraceResult trace )
		{
			
		}
	}
}
