using Sandbox;
using System;

namespace Facepunch.Hidden
{
	[Library]
	public class KnifeConfig : WeaponConfig
	{
		public override string Name => "Knife";
		public override string Description => "Brutal slashing melee weapon";
		public override string ClassName => "hdn_knife";
		public override string Icon => "ui/weapons/knife.png";
		public override WeaponType Type => WeaponType.Hitscan;
		public override int Ammo => 0;
		public override int Damage => 35;
	}

	[Library( "hdn_knife" )]
	public partial class Knife : Weapon
	{
		public override string ViewModelPath => "weapons/rust_boneknife/v_rust_boneknife.vmdl";
		public override WeaponConfig Config => new KnifeConfig();

		public override float PrimaryRate => 1.0f;
		public override float SecondaryRate => 0.3f;
		public override bool IsMelee => true;
		public override int HoldType => 0;
		public override float MeleeRange => 80f;
		public override bool HasLaserDot => false;
		public override bool HasFlashlight => false;

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "weapons/rust_boneknife/rust_boneknife.vmdl" );
		}

		public override void AttackSecondary()
		{
			StartChargeAttack();
		}

		public override void AttackPrimary()
		{
			ShootEffects();
			PlaySound( "rust_boneknife.attack" );
			MeleeStrike( Config.Damage, 1.5f );
		}

		public override void OnChargeAttackFinish()
		{
			ShootEffects();
			PlaySound( "rust_boneknife.attack" );
			MeleeStrike( Config.Damage * 3f, 1.5f );
		}
	}
}
