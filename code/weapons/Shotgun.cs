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

			(Owner as AnimatedEntity).SetAnimParameter( "b_attack", true );

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

			//CrosshairPanel?.CreateEvent( "fire" );
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

		public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
		{
			var draw = Render.Draw2D;

			var color = Color.Lerp( Color.Red, Color.Yellow, lastReload.LerpInverse( 0.0f, 0.4f ) );
			draw.BlendMode = BlendMode.Lighten;
			draw.Color = color.WithAlpha( 0.2f + lastAttack.LerpInverse( 1.2f, 0 ) * 0.5f );

			// center
			{
				var shootEase = 1 + Easing.BounceIn( lastAttack.LerpInverse( 0.3f, 0.0f ) );
				draw.Ring( center, 15 * shootEase, 14 * shootEase );
			}

			// outer lines
			{
				var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.4f, 0.0f ) );
				var length = 30.0f;
				var gap = 30.0f + shootEase * 50.0f;
				var thickness = 4.0f;
				var extraAngle = 30 * shootEase;

				draw.CircleEx( center + Vector2.Right * gap, length, length - thickness, 32, 220, 320 );
				draw.CircleEx( center - Vector2.Right * gap, length, length - thickness, 32, 40, 140 );

				draw.Color = draw.Color.WithAlpha( 0.1f );
				draw.CircleEx( center + Vector2.Right * gap * 2.6f, length, length - thickness * 0.5f, 32, 220, 320 );
				draw.CircleEx( center - Vector2.Right * gap * 2.6f, length, length - thickness * 0.5f, 32, 40, 140 );
			}
		}
	}
}
