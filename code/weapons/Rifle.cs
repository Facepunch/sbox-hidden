using Sandbox;
using System;

namespace Facepunch.Hidden
{
	[Library]
	public class RifleConfig : WeaponConfig
	{
		public override string Name => "FAMAS";
		public override string Description => "Long-range hitscan burst mode rifle";
		public override string ClassName => "hdn_rifle";
		public override string Icon => "ui/weapons/rifle.png";
		public override AmmoType AmmoType => AmmoType.Rifle;
		public override WeaponType Type => WeaponType.Hitscan;
		public override int Ammo => 0;
		public override int Damage => 24;
	}

	[Library( "hdn_rifle" )]
	public partial class Rifle : Weapon
	{
		public override string ViewModelPath => "models/f1/fp_f1.vmdl";
		public override WeaponConfig Config => new RifleConfig();

		public override int ClipSize => 25;
		public override float PrimaryRate => 1f;
		public override float SecondaryRate => 1.0f;
		public override float DamageFalloffStart => 1000f;
		public override float DamageFalloffEnd => 12000f;
		public override float ReloadTime => 3.0f;
		public override bool HasLaserDot => true;
		public override AnimationHelperWithLegs.HoldTypes HoldType => AnimationHelperWithLegs.HoldTypes.Rifle;

		public virtual int BulletsPerBurst => 3;

		[Net, Predicted] private TimeSince LastBulletTime { get; set; }
		[Net, Predicted] private bool FireBulletNow { get; set; }
		[Net, Predicted] private int BulletsToFire { get; set; }

		[ClientRpc]
		protected override void ShootEffects()
		{
			Game.AssertClient();

			base.ShootEffects();

			Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
			Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
		}

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/f1/w_f1.vmdl" );
		}

		public override void PlayReloadSound()
		{
			PlaySound( "rust_pistol.reload" );
			base.PlayReloadSound();
		}

		public override void AttackPrimary()
		{
			if ( AmmoClip == 0 )
			{
				PlaySound( "pistol.dryfire" );
				return;
			}

			BulletsToFire += Math.Min( AmmoClip, BulletsPerBurst );
			FireBulletNow = true;
			AmmoClip = Math.Max( AmmoClip - BulletsPerBurst, 0 );
		}

		public override void CreateViewModel()
		{
			base.CreateViewModel();

			if ( ViewModelEntity is ViewModel vm )
			{
				var aimConfig = new ViewModelAimConfig
				{
					Position = Vector3.Backward * 20f + Vector3.Left * 18f + Vector3.Up * 4f,
					Speed = 1f
				};

				vm.AimConfig = aimConfig;
			}
		}

		public override void Simulate( IClient owner )
		{
			base.Simulate( owner );

			if ( Game.IsClient && ViewModelEntity is ViewModel vm )
			{
				//vm.IsAiming = Input.Down( "attack2" );
			}

			if ( BulletsToFire > 0 && (LastBulletTime > 0.075f || FireBulletNow) )
			{
				using ( LagCompensation() )
				{
					FireBulletNow = false;
					BulletsToFire--;

					Game.SetRandomSeed( Time.Tick );

					ShootEffects();
					PlaySound( $"ar2_shoot" );
					ShootBullet( 0.015f, 1.5f, Config.Damage, 8.0f );
					PlayAttackAnimation();
					AddRecoil( new Angles( Game.Random.Float( -0.3f, -0.6f ), 0f, 0f ) );

					LastBulletTime = 0f;
				}
			}
		}
	}
}
