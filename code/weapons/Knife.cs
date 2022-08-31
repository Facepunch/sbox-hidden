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
		public override int Damage => 40;
	}

	[Library( "hdn_knife" )]
	public partial class Knife : Weapon
	{
		public override string ViewModelPath => "models/knife/fp_knife.vmdl";
		public override WeaponConfig Config => new KnifeConfig();

		public override float PrimaryRate => 1f;
		public override float SecondaryRate => 0f;
		public override bool IsMelee => true;
		public override int HoldType => 0;
		public override float MeleeRange => 80f;
		public override bool HasLaserDot => false;
		public override bool HasFlashlight => false;

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/knife/w_knife.vmdl" );
		}

		public override void AttackSecondary()
		{
			if ( Owner is Player player && player.Stamina > 20f )
			{
				player.StaminaRegenTime = 1f;
				player.Stamina = Math.Max( player.Stamina - 80f, 0f );
				player.PlaySound( "pigstick.whisper" );

				StartChargeAttack();
			}
		}

		public override void AttackPrimary()
		{
			ShootEffects();
			PlaySound( "knife.swipe" );
			MeleeStrike( Config.Damage, 1.5f );
		}

		public override void OnChargeAttackFinish()
		{
			ShootEffects();
			PlaySound( "knife.swipe" );
			MeleeStrike( Config.Damage * 3f, 2f );
		}

		protected override void OnMeleeStrikeHit( Entity entity, DamageInfo info )
		{
			if ( entity is Player player )
			{
				if ( info.Damage > Config.Damage * 1.5f )
					Sound.FromEntity( "pigstick.slash", player );
				else
					Sound.FromEntity( "slash", player );
			}
			else if ( entity is PlayerCorpse corpse )
			{
				if ( corpse.NumberOfFeedsLeft > 0 )
				{
					if ( Owner is Player owner && owner.Health < 100f )
					{
						owner.Health = Math.Min( owner.Health + 15f, 100f );
						Sound.FromEntity( "hidden.feed", owner );

						corpse.NumberOfFeedsLeft--;

						if ( corpse.NumberOfFeedsLeft == 0 )
						{
							corpse.SetMaterialGroup( 5 );
						}
					}
				}

				Sound.FromEntity( "slash", corpse );
			}
			else
			{
				Sound.FromWorld( "knife.slash", info.Position );
			}
		}

		[Event.Frame]
		protected virtual void OnFrame()
		{
			EnableDrawing = false;
		}
	}
}
