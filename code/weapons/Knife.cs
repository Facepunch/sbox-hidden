using Sandbox;
using System;

namespace HiddenGamemode
{
	[ClassLibrary( "hdn_knife", Title = "Knife" )]
	partial class Knife : Weapon
	{
		public override string ViewModelPath => "weapons/rust_boneknife/v_rust_boneknife.vmdl";
		public override float PrimaryRate => 1.0f;
		public override float SecondaryRate => 1.0f;
		public override bool IsMelee => true;
		public override int HoldType => 0;
		public override int Bucket => 1;
		public virtual int MeleeDistance => 80;

		private PhysicsBody _ragdollBody;
		private WeldJoint _ragdollWeld;

		public override void ActiveEnd( Entity owner, bool wasDropped )
		{
			if ( _ragdollWeld.IsValid() )
			{
				_ragdollWeld.Remove();
			}

			base.ActiveEnd( owner, wasDropped );
		}

		public override void Spawn()
		{
			base.Spawn();

			// TODO: EnableDrawing = false does not work.
			RenderAlpha = 0f;

			SetModel( "weapons/rust_boneknife/rust_boneknife.vmdl" );
		}

		public virtual void MeleeStrike( float damage, float force )
		{
			var forward = Owner.EyeRot.Forward;
			forward = forward.Normal;

			foreach ( var tr in TraceBullet( Owner.EyePos, Owner.EyePos + forward * MeleeDistance, 3f ) )
			{
				if ( !tr.Entity.IsValid() ) continue;

				tr.Surface.DoBulletImpact( tr );

				if ( !IsServer ) continue;

				using ( Prediction.Off() )
				{
					var damageInfo = DamageInfo.FromBullet( tr.EndPos, forward * 100 * force, damage )
						.UsingTraceResult( tr )
						.WithAttacker( Owner )
						.WithWeapon( this );

					tr.Entity.TakeDamage( damageInfo );
				}
			}
		}

		public override bool CanPrimaryAttack( Sandbox.Player owner )
		{
			return base.CanPrimaryAttack( owner ) && owner.Input.Pressed( InputButton.Attack1 );
		}

		public override void AttackSecondary( Sandbox.Player owner )
		{
			TimeSinceSecondaryAttack = 0;

			var player = (owner as Player);
			var controller = (player.Controller as HiddenController);

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

		public override void AttackPrimary( Sandbox.Player owner )
		{
			TimeSincePrimaryAttack = 0;

			if ( IsServer )
			{
				ProcessRagdollPickup( owner );
			}

			ShootEffects();
			PlaySound( "rust_boneknife.attack" );
			MeleeStrike( 25f, 10f );
		}

		private void ProcessRagdollPickup( Sandbox.Player owner )
		{
			var trace = Trace.Ray( owner.EyePos, owner.EyePos + owner.EyeRot.Forward * 80f )
				.HitLayer( CollisionLayer.Debris )
				.Ignore( owner )
				.Ignore( this )
				.Radius( 1 )
				.Run();

			var didPickupRagdoll = false;

			if ( trace.Hit )
			{
				if ( !_ragdollWeld.IsValid() )
				{
					var iris = Game.Instance.GetTeamPlayers<IrisTeam>();

					iris.ForEach( ( player ) =>
					{
						if ( player.RagdollEntity == trace.Entity )
						{
							if ( IsServer )
							{
								using ( Prediction.Off() )
								{
									_ragdollBody = trace.Body;
									_ragdollWeld = PhysicsJoint.Weld
										.From( owner.PhysicsBody, owner.PhysicsBody.Transform.PointToLocal( owner.EyePos + owner.EyeRot.Forward * 40f ) )
										.To( trace.Body, trace.Body.Transform.PointToLocal( trace.EndPos ) )
										.WithLinearSpring( 20f, 1f, 0.0f )
										.WithAngularSpring( 0.0f, 0.0f, 0.0f )
										.Create();

									didPickupRagdoll = true;
								}
							}
						}
					} );
				}
			}

			if ( !didPickupRagdoll && _ragdollWeld.IsValid() )
			{
				trace = Trace.Ray( owner.EyePos, owner.EyePos + owner.EyeRot.Forward * 40f )
					.HitLayer( CollisionLayer.WORLD_GEOMETRY )
					.Ignore( owner )
					.Ignore( this )
					.Radius( 1 )
					.Run();

				if ( trace.Hit && _ragdollBody != null && _ragdollBody.IsValid() )
				{
					// TODO: This should be a weld joint to the world but it doesn't work right now.
					_ragdollBody.BodyType = PhysicsBodyType.Static;
					_ragdollBody.Pos = trace.EndPos - (trace.Direction * 2.5f);
				}

				_ragdollWeld.Remove();
			}
		}
	}
}
