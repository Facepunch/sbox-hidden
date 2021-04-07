using Sandbox;
using System;

namespace HiddenGamemode
{
	[ClassLibrary( "hdn_knife", Title = "Knife" )]
	partial class Knife : Weapon
	{
		public override string ViewModelPath => "weapons/rust_boneknife/v_rust_boneknife.vmdl";
		public override float PrimaryRate => 15.0f;
		public override float SecondaryRate => 1.0f;
		public override bool IsMelee => true;
		public override int Bucket => 1;

		public override void Spawn()
		{
			base.Spawn();

			SetModel( "weapons/rust_boneknife/rust_boneknife.vmdl" );
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

			ShootEffects();
			//PlaySound( "rust_pistol.shoot" );
			//ShootBullet( 0.05f, 1.5f, 9.0f, 3.0f );
		}
	}
}
