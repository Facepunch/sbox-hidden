using Sandbox;

namespace Facepunch.Hidden
{
	[Library]
	public class ShotgunConfig : WeaponConfig
	{
		public override string Name => "SPAS-12";
		public override string Description => "Short-range hitscan shotgun";
		public override string ClassName => "hdn_shotgun";
		public override string Icon => "ui/weapons/shotgun.png";
		public override AmmoType AmmoType => AmmoType.Shotgun;
		public override WeaponType Type => WeaponType.Hitscan;
		public override int Ammo => 0;
		public override int Damage => 4;
	}

	[Library( "hdn_shotgun" )]
	public partial class Shotgun : Weapon
	{
		public override string ViewModelPath => "weapons/rust_pumpshotgun/v_rust_pumpshotgun.vmdl";
		public override WeaponConfig Config => new ShotgunConfig();

		public override float PrimaryRate => 1f;
		public override float SecondaryRate => 1f;
		public override int ClipSize => 8;
		public override float ReloadTime => 0.5f;
		public override bool HasLaserDot => true;
		public override bool HasFlashlight => true;

		[ClientRpc]
		protected override void ShootEffects()
		{
			Host.AssertClient();

			base.ShootEffects();

			Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
			Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
		}

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

			Rand.SetSeed( Time.Tick );

			ShootEffects();
			PlaySound( $"rust_pumpshotgun.shoot" );

			for ( int i = 0; i < 10; i++ )
			{
				ShootBullet( 0.15f, 0.3f, Config.Damage, 3.0f );
			}

			PlayAttackAnimation();
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
				var ammo = player.TakeAmmo( Config.AmmoType, 1 );
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
