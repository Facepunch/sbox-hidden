using Sandbox;

namespace HiddenGamemode
{
	[Library( "hdn_smg", Title = "MP5" )]
	partial class SMG : Weapon
	{
		public override string ViewModelPath => "weapons/rust_smg/v_rust_smg.vmdl";

		public override float PrimaryRate => 10.0f;
		public override float SecondaryRate => 1.0f;
		public override int ClipSize => 30;
		public override float ReloadTime => 4.0f;
		public override bool HasFlashlight => true;
		public override bool HasLaserDot => true;
		public override int BaseDamage => 5;
		public override int Bucket => 2;

		public override void Spawn()
		{
			base.Spawn();

			SetModel( "weapons/rust_smg/rust_smg.vmdl" );
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
			PlaySound( "rust_smg.shoot" );
			ShootBullet( 0.1f, 1.5f, BaseDamage, 3.0f );
		}

		[ClientRpc]
		protected override void ShootEffects()
		{
			Host.AssertClient();

			Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
			Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );

			ViewModelEntity?.SetAnimParameter( "fire", true );
		}

		public override void SimulateAnimator( PawnAnimator anim )
		{
			anim.SetAnimParameter( "holdtype", 2 ); // TODO this is shit
			anim.SetAnimParameter( "aim_body_weight", 1.0f );
		}

		public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
		{
			var draw = Render.Draw2D;

			var color = Color.Lerp( Color.Red, Color.Yellow, lastReload.LerpInverse( 0.0f, 0.4f ) );
			draw.BlendMode = BlendMode.Lighten;
			draw.Color = color.WithAlpha( 0.2f + CrosshairLastShoot.Relative.LerpInverse( 1.2f, 0 ) * 0.5f );

			// center circle
			{
				var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.1f, 0.0f ) );
				var length = 2.0f + shootEase * 2.0f;
				draw.Circle( center, length );
			}


			draw.Color = draw.Color.WithAlpha( draw.Color.a * 0.2f );

			// outer lines
			{
				var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.2f, 0.0f ) );
				var length = 3.0f + shootEase * 2.0f;
				var gap = 30.0f + shootEase * 50.0f;
				var thickness = 2.0f;

				draw.Line( thickness, center + Vector2.Up * gap + Vector2.Left * length, center + Vector2.Up * gap - Vector2.Left * length );
				draw.Line( thickness, center - Vector2.Up * gap + Vector2.Left * length, center - Vector2.Up * gap - Vector2.Left * length );

				draw.Line( thickness, center + Vector2.Left * gap + Vector2.Up * length, center + Vector2.Left * gap - Vector2.Up * length );
				draw.Line( thickness, center - Vector2.Left * gap + Vector2.Up * length, center - Vector2.Left * gap - Vector2.Up * length );
			}
		}
	}
}
