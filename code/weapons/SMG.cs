using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	[Library]
	public class SMGConfig : WeaponConfig
	{
		public override string Name => "MP5";
		public override string Description => "Medium-range hitscan SMG";
		public override string ClassName => "hdn_smg";
		public override string Icon => "ui/weapons/smg.png";
		public override AmmoType AmmoType => AmmoType.SMG;
		public override WeaponType Type => WeaponType.Hitscan;
		public override int Ammo => 0;
		public override int Damage => 16;
	}

	[Library( "hdn_smg" )]
	public partial class SMG : Weapon
	{
		public override string ViewModelPath => "models/mp5/fp_mp5.vmdl";
		public override WeaponConfig Config => new SMGConfig();

		public override float PrimaryRate => 10f;
		public override float SecondaryRate => 1f;
		public override float DamageFalloffStart => 500f;
		public override float DamageFalloffEnd => 8000f;
		public override int ClipSize => 30;
		public override float ReloadTime => 4.0f;
		public override bool HasFlashlight => true;
		public override bool HasLaserDot => true;
		public override int HoldType => 2;

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
			SetModel( "models/mp5/w_mp5.vmdl" );
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

			Rand.SetSeed( Time.Tick );

			ShootEffects();
			PlaySound( $"smg1_shoot" );
			ShootBullet( 0.01f, 1.5f, Config.Damage, 8.0f );
			PlayAttackAnimation();
			AddRecoil( new Angles( Rand.Float( -0.75f, -1f ), 0f, 0f ) );
		}
	}
}
