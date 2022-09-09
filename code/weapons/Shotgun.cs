using Sandbox;

namespace Facepunch.Hidden
{
	[Library]
	public class ShotgunConfig : WeaponConfig
	{
		public override string Name => "R870";
		public override string Description => "Short-range hitscan shotgun";
		public override string ClassName => "hdn_shotgun";
		public override string Icon => "ui/weapons/shotgun.png";
		public override AmmoType AmmoType => AmmoType.Shotgun;
		public override WeaponType Type => WeaponType.Hitscan;
		public override int Ammo => 0;
		public override int Damage => 20;
	}

	[Library( "hdn_shotgun" )]
	public partial class Shotgun : Weapon
	{
		public override string ViewModelPath => "models/r870/fp_r870.vmdl";
		public override WeaponConfig Config => new ShotgunConfig();

		public override float PrimaryRate => 1f;
		public override float SecondaryRate => 1f;
		public override int ClipSize => 8;
		public override int HoldType => 3;
		public override float DamageFalloffStart => 0f;
		public override float DamageFalloffEnd => 1024f;
		public override float ReloadTime => .5f;
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
			SetModel( "models/r870/w_r870.vmdl" );
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
			PlaySound( $"shotgun1_shoot" );

			for ( int i = 0; i < 8; i++ )
			{
				ShootBullet( 0.3f, 0.7f, Config.Damage, 3.0f );
			}

			PlayAttackAnimation();
			AddRecoil( new Angles( Rand.Float( -2f, -3f ), 0f, 0f ) );
		}

		public override void OnReloadFinish()
		{
			IsReloading = false;
			TimeSinceReload = 0f;
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
	}
}
