using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	[Library]
	public class SMGConfig : WeaponConfig
	{
		public override string Name => "SMG";
		public override string Description => "Medium-range hitscan SMG";
		public override string ClassName => "hdn_smg";
		public override string Icon => "ui/weapons/smg.png";
		public override AmmoType AmmoType => AmmoType.SMG;
		public override WeaponType Type => WeaponType.Hitscan;
		public override int Ammo => 0;
		public override int Damage => 4;
	}

	[Library( "hdn_smg" )]
	public partial class SMG : Weapon
	{
		public override string ViewModelPath => "weapons/rust_smg/v_rust_smg.vmdl";
		public override WeaponConfig Config => new SMGConfig();

		public override float PrimaryRate => 10f;
		public override float SecondaryRate => 1f;
		public override int ClipSize => 30;
		public override float ReloadTime => 4.0f;
		public override bool HasFlashlight => true;
		public override bool HasLaserDot => true;

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
			SetModel( "weapons/rust_smg/rust_smg.vmdl" );
		}

		public override void PlayReloadSound()
		{
			PlaySound( "rust_smg.reload" );
			base.PlayReloadSound();
		}

		public override void AttackPrimary()
		{
			if ( !TakeAmmo( 1 ) )
			{
				PlaySound( "pistol.dryfire" );
				return;
			}

			TimeSincePrimaryAttack = 0f;

			Rand.SetSeed( Time.Tick );

			ShootEffects();
			PlaySound( $"rust_smg.shoot" );
			ShootBullet( 0.01f, 1.5f, Config.Damage, 8.0f );
			PlayAttackAnimation();
		}
	}
}
