using Sandbox;
using System;

namespace Facepunch.Hidden
{
	[Library]
	public class PistolConfig : WeaponConfig
	{
		public override string Name => "M1911";
		public override string Description => "Short-range hitscan pistol";
		public override string ClassName => "hdn_pistol";
		public override string Icon => "ui/weapons/pistol.png";
		public override AmmoType AmmoType => AmmoType.Pistol;
		public override WeaponType Type => WeaponType.Hitscan;
		public override int Ammo => 0;
		public override int Damage => 20;
	}

	[Library( "hdn_pistol" )]
	public partial class Pistol : Weapon
	{
		public override string ViewModelPath => "models/m1911/fp_m1911.vmdl";
		public override WeaponConfig Config => new PistolConfig();

		public override bool UnlimitedAmmo => true;
		public override int ClipSize => 10;
		public override float PrimaryRate => 3f;
		public override float SecondaryRate => 1.0f;
		public override float DamageFalloffStart => 500f;
		public override float DamageFalloffEnd => 8000f;
		public override float ReloadTime => 3.0f;
		public override bool HasLaserDot => true;
		public override AnimationHelperWithLegs.HoldTypes HoldType => AnimationHelperWithLegs.HoldTypes.Pistol;

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
			SetModel( "models/m1911/w_m1911.vmdl" );
		}

		public override void PlayReloadSound()
		{
			PlaySound( "rust_pistol.reload" );
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
			PlaySound( $"pistol_shoot" );
			ShootBullet( 0.01f, 1.5f, Config.Damage, 8.0f );
			PlayAttackAnimation();
			AddRecoil( new Angles( Rand.Float( -0.75f, -1f ), 0f, 0f ) );
		}
	}
}
