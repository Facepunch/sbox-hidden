using Sandbox;
using System;

namespace HiddenGamemode
{
	[ClassLibrary( "hdn_pistol", Title = "Baretta" )]
	partial class Pistol : Weapon
	{
		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

		public override float PrimaryRate => 15.0f;
		public override float SecondaryRate => 1.0f;
		public override float ReloadTime => 3.0f;
		public override int Bucket => 1;

		public override void Spawn()
		{
			base.Spawn();

			SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );

			AmmoClip = 12;
		}

		public override bool CanPrimaryAttack( Sandbox.Player owner )
		{
			return base.CanPrimaryAttack( owner ) && owner.Input.Pressed( InputButton.Attack1 );
		}

		public override void AttackSecondary( Sandbox.Player owner )
		{
			TimeSinceSecondaryAttack = 0;

			if (IsServer)
			{
				var player = (owner as Player);
				var controller = (player.Controller as HiddenController);

				using ( Prediction.Off() )
				{
					if ( controller.IsFrozen )
					{
						controller.WishVelocity = Vector3.Zero;
						controller.Velocity = owner.EyeRot.Forward * 400f;
						controller.IsFrozen = false;
						return;
					}

					var trace = Trace.Ray( owner.EyePos, owner.EyePos + owner.EyeRot.Forward * 40f )
						.HitLayer( CollisionLayer.WORLD_GEOMETRY )
						.Ignore( owner )
						.Ignore( this )
						.Radius( 1 )
						.Run();

					if ( trace.Hit )
					{
						if ( controller != null )
						{
							controller.IsFrozen = true;
						}
					}
				}
			}
		}

		public override void AttackPrimary( Sandbox.Player owner )
		{
			TimeSincePrimaryAttack = 0;

			if ( !TakeAmmo( 1 ) )
			{
				DryFire();
				return;
			}

			ShootEffects();
			PlaySound( "rust_pistol.shoot" );
			ShootBullet( 0.05f, 1.5f, 9.0f, 3.0f );
		}
	}
}
