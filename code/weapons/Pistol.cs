using Sandbox;
using System;

namespace HiddenGamemode
{
	[Library( "hdn_pistol", Title = "Baretta" )]
	partial class Pistol : Weapon
	{
		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

		public override bool UnlimitedAmmo => true;
		public override int ClipSize => 10;
		public override float PrimaryRate => 15.0f;
		public override float SecondaryRate => 1.0f;
		public override float ReloadTime => 3.0f;
		public override bool HasLaserDot => true;
		public override int BaseDamage => 8;
		public override int Bucket => 1;

		public override void Spawn()
		{
			base.Spawn();

			SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
		}

		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && Input.Pressed( InputButton.PrimaryAttack );
		}

		public override void AttackPrimary()
		{
			if ( !TakeAmmo( 1 ) )
			{
				PlaySound( "pistol.dryfire" );
				return;
			}

			ShootEffects();
			PlaySound( "rust_pistol.shoot" );
			ShootBullet( 0.05f, 1.5f, BaseDamage, 3.0f );
		}

		public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
		{
			var draw = Render.Draw2D;

			var shootEase = Easing.EaseIn( lastAttack.LerpInverse( 0.2f, 0.0f ) );
			var color = Color.Lerp( Color.Red, Color.Yellow, lastReload.LerpInverse( 0.0f, 0.4f ) );

			draw.BlendMode = BlendMode.Lighten;
			draw.Color = color.WithAlpha( 0.2f + lastAttack.LerpInverse( 1.2f, 0 ) * 0.5f );

			var length = 8.0f - shootEase * 2.0f;
			var gap = 10.0f + shootEase * 30.0f;
			var thickness = 2.0f;

			draw.Line( thickness, center + Vector2.Left * gap, center + Vector2.Left * (length + gap) );
			draw.Line( thickness, center - Vector2.Left * gap, center - Vector2.Left * (length + gap) );

			draw.Line( thickness, center + Vector2.Up * gap, center + Vector2.Up * (length + gap) );
			draw.Line( thickness, center - Vector2.Up * gap, center - Vector2.Up * (length + gap) );
		}
	}
}
