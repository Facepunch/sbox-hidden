using Sandbox;


namespace HiddenGamemode
{
	[Library( "hdn_shotgun", Title = "SPAS-12" )]
	partial class Shotgun : Weapon
	{
		public override string ViewModelPath => "weapons/rust_pumpshotgun/v_rust_pumpshotgun.vmdl";
		public override float PrimaryRate => 1;
		public override float SecondaryRate => 1;
		public override AmmoType AmmoType => AmmoType.Buckshot;
		public override int ClipSize => 8;
		public override float ReloadTime => 0.5f;
		public override bool HasLaserDot => true;
		public override bool HasFlashlight => true;
		public override int BaseDamage => 6; // This is per bullet, so 6 x 10 for the shotgun.
		public override int Bucket => 3;

		public override void Spawn()
		{
			base.Spawn();

			SetModel( "weapons/rust_pumpshotgun/rust_pumpshotgun.vmdl" );
		}

		public override void AttackPrimary()
		{
			if ( !TakeAmmo( 1 ) )
			{
				PlaySound( "pistol.dryfire" );
				return;
			}

			(Owner as AnimEntity).SetAnimParameter( "b_attack", true );

			ShootEffects();
			PlaySound( "rust_pumpshotgun.shoot" );

			for ( int i = 0; i < 10; i++ )
			{
				ShootBullet( 0.15f, 0.3f, BaseDamage, 3.0f );
			}
		}

		[ClientRpc]
		protected override void ShootEffects()
		{
			Host.AssertClient();

			Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
			Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );

			ViewModelEntity?.SetAnimParameter( "fire", true );

			if ( IsLocalPawn )
			{
				_ = new Sandbox.ScreenShake.Perlin( 1.0f, 1.5f, 2.0f );
			}

			CrosshairPanel?.CreateEvent( "fire" );
		}

		public override void OnReloadFinish()
		{
			IsReloading = false;

			TimeSincePrimaryAttack = 0;
			TimeSinceSecondaryAttack = 0;

			if ( AmmoClip >= ClipSize )
				return;

			if ( Owner is Player player )
			{
				var ammo = player.TakeAmmo( AmmoType, 1 );
				if ( ammo == 0 )
					return;

				AmmoClip += ammo;

				if ( AmmoClip < ClipSize )
				{
					Reload();
				}
				else
				{
					FinishReload();
				}
			}
		}

		[ClientRpc]
		protected virtual void FinishReload()
		{
			ViewModelEntity?.SetAnimParameter( "reload_finished", true );
		}

		public override void SimulateAnimator( PawnAnimator anim )
		{
			anim.SetAnimParameter( "holdtype", 2 );
			anim.SetAnimParameter( "aim_body_weight", 1.0f );
		}
	}
}
