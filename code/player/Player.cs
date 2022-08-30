using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	public partial class Player : Sandbox.Player
	{
		[Net, Predicted] public float Stamina { get; set; }
		[Net, Local] public SenseAbility Sense { get; set; }
		[Net, Local] public ScreamAbility Scream { get; set; }
		[Net, Local] public DeploymentType Deployment { get; set; }

		public ProjectileSimulator Projectiles { get; private set; }

		private Rotation LastCameraRotation = Rotation.Identity;
		private DamageInfo LastDamageInfo;
		private PhysicsBody RagdollBody;
		private PhysicsJoint RagdollWeld;
		private Particles SenseParticles;
		private float WalkBob = 0;
		private float Lean = 0;
		private float FOV = 0;

		public bool HasTeam
		{
			get => Team != null;
		}

		public Player()
		{
			Projectiles = new( this );
			Inventory = new Inventory( this );
			Animator = new StandardPlayerAnimator();
			Ammo = new List<int>();
		}

		public bool IsSpectator
		{
			get => (CameraMode is SpectateCamera);
		}

		public Vector3 SpectatorDeathPosition
		{
			get
			{
				if ( CameraMode is SpectateCamera camera )
					return camera.DeathPosition;

				return Vector3.Zero;
			}
		}

		public bool HasSpectatorTarget
		{
			get
			{
				var target = SpectatorTarget;
				return (target != null && target.IsValid());
			}
		}

		public Player SpectatorTarget
		{
			get
			{
				if ( CameraMode is SpectateCamera camera )
					return camera.TargetPlayer;

				return null;
			}
		}

		public void MakeSpectator( Vector3 position = default )
		{
			EnableAllCollisions = false;
			EnableDrawing = false;
			Controller = null;
			CameraMode = new SpectateCamera
			{
				DeathPosition = position,
				TimeSinceDied = 0
			};
		}

		public override void Respawn()
		{
			Game.Instance?.Round?.OnPlayerSpawn( this );

			RemoveRagdollEntity();
			DrawPlayer( true );

			RagdollWeld = null;
			RagdollBody = null;

			Stamina = 100f;

			base.Respawn();
		}

		public override void OnKilled()
		{
			base.OnKilled();

			ShowFlashlight( false, false );
			ShowSenseParticles( false );
			DrawPlayer( false );

			BecomeRagdollOnServer( LastDamageInfo.Force, GetHitboxBone( LastDamageInfo.HitboxIndex ) );

			Inventory.DeleteContents();

			Team?.OnPlayerKilled( this );
		}

		public override void FrameSimulate( Client client )
		{
			SimulateLaserDot( client );

			base.FrameSimulate( client );
		}

		public override void Simulate( Client client )
		{
			Projectiles.Simulate();

			SimulateActiveChild( client, ActiveChild );
			TickFlashlight();

			if ( Input.ActiveChild != null )
			{
				ActiveChild = Input.ActiveChild;
			}

			if ( LifeState != LifeState.Alive )
			{
				if ( IsServer )
					DestroyLaserDot();

				return;
			}

			TickPlayerUse();

			if ( IsServer )
			{
				using ( Prediction.Off() )
				{
					//TickPickupRagdoll();
					SimulateLaserDot( client );
				}
			}

			if ( Team != null )
			{
				Team.OnTick( this );
			}

			if ( ActiveChild is Weapon weapon && !weapon.IsUsable() && weapon.TimeSincePrimaryAttack > 0.5f && weapon.TimeSinceSecondaryAttack > 0.5f )
			{
				SwitchToBestWeapon();
			}

			var controller = GetActiveController();
			controller?.Simulate( client, this, GetActiveAnimator() );
		}

		protected override void UseFail()
		{
			// Do nothing. By default this plays a sound that we don't want.
		}

		public void DrawPlayer( bool shouldDraw )
		{
			EnableDrawing = shouldDraw;
			Clothing.ForEach( x => x.EnableDrawing = shouldDraw );
		}

		public void ShowSenseParticles( bool shouldShow )
		{
			if ( SenseParticles != null )
			{
				SenseParticles.Destroy( false );
				SenseParticles = null;
			}

			if ( shouldShow )
			{
				SenseParticles = Particles.Create( "particles/sense.vpcf" );

				if ( SenseParticles != null )
					SenseParticles.SetEntity( 0, this, true );
			}
		}

		public void SwitchToBestWeapon()
		{
			var best = Children.Select( x => x as Weapon )
				.Where( x => x.IsValid() && x.IsUsable() )
				.FirstOrDefault();

			if ( best == null ) return;

			ActiveChild = best;
		}

		public override void OnActiveChildChanged( Entity from, Entity to )
		{
			if ( to is Weapon && HasFlashlightEntity )
			{
				ShowFlashlight( false );
			}

			base.OnActiveChildChanged( from, to );
		}

		public override void PostCameraSetup( ref CameraSetup setup )
		{
			base.PostCameraSetup( ref setup );

			if ( LastCameraRotation == Rotation.Identity )
				LastCameraRotation = CurrentView.Rotation;

			var angleDiff = Rotation.Difference( LastCameraRotation, CurrentView.Rotation );
			var angleDiffDegrees = angleDiff.Angle();
			var allowance = 20.0f;

			if ( angleDiffDegrees > allowance )
			{
				LastCameraRotation = Rotation.Lerp( LastCameraRotation, CurrentView.Rotation, 1.0f - (allowance / angleDiffDegrees) );
			}

			if ( CameraMode is FirstPersonCamera camera )
			{
				AddCameraEffects( camera );
			}
		}

		private void TickPickupRagdoll()
		{
			if ( !Input.Pressed( InputButton.Use ) ) return;

			var trace = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 80f )
				.EntitiesOnly()
				.Ignore( ActiveChild )
				.Ignore( this )
				.Radius( 2 )
				.Run();

			if ( trace.Hit && trace.Entity is PlayerCorpse corpse && corpse.Player != null )
			{
				if ( !RagdollWeld.IsValid() )
				{
					RagdollBody = trace.Body;
					RagdollWeld = PhysicsJoint.CreateLength( PhysicsPoint.Local( PhysicsBody, Vector3.Up * 2f ), PhysicsPoint.World( trace.Body, trace.EndPosition ), 20f );
					return;
				}
			}

			if ( RagdollBody.IsValid() )
			{
				trace = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 40f )
					.WorldOnly()
					.Ignore( ActiveChild )
					.Ignore( this )
					.Radius( 2 )
					.Run();

				if ( trace.Hit && RagdollBody != null && RagdollBody.IsValid() )
				{
					PhysicsJoint.CreateLength( PhysicsPoint.World( Map.Physics.Body, trace.EndPosition ), PhysicsPoint.World( RagdollBody, trace.EndPosition ), 4f );
				}

				RagdollWeld.Remove();
				RagdollBody = null;
				RagdollWeld = null;
			}
		}

		private void AddCameraEffects( CameraMode camera )
		{
			var speed = Velocity.Length.LerpInverse( 0, 320 );
			var forwardspeed = Velocity.Normal.Dot( camera.Rotation.Forward );

			var left = camera.Rotation.Left;
			var up = camera.Rotation.Up;

			if ( GroundEntity != null )
			{
				WalkBob += Time.Delta * 25.0f * speed;
			}

			camera.Position += up * MathF.Sin( WalkBob ) * speed * 2;
			camera.Position += left * MathF.Sin( WalkBob * 0.6f ) * speed * 1;

			Lean = Lean.LerpTo( Velocity.Dot( camera.Rotation.Right ) * 0.01f, Time.Delta * 15.0f );

			var appliedLean = Lean;
			appliedLean += MathF.Sin( WalkBob ) * speed * 0.3f;
			camera.Rotation *= Rotation.From( 0, 0, appliedLean );

			speed = (speed - 0.7f).Clamp( 0, 1 ) * 3.0f;

			FOV = FOV.LerpTo( speed * 20 * MathF.Abs( forwardspeed ), Time.Delta * 4.0f );

			camera.FieldOfView += FOV;
		}

		public override void TakeDamage( DamageInfo info )
		{
			if ( info.HitboxIndex == 0 )
			{
				info.Damage *= 2.0f;
			}

			if ( info.Attacker is Player attacker && attacker != this )
			{
				if ( !Game.FriendlyFire && attacker.Team == Team )
				{
					return;
				}

				Team?.OnTakeDamageFromPlayer( this, attacker, info );
				attacker.Team?.OnDealDamageToPlayer( attacker, this, info );
				attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, ((float)Health).LerpInverse( 100, 0 ) );
			}

			TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position, info.Flags );

			if ( info.Flags.HasFlag( DamageFlags.Fall ) )
			{
				PlaySound( "fall" );
			}
			else if ( info.Flags.HasFlag( DamageFlags.Bullet ) )
			{
				if ( !Team?.PlayPainSounds( this ) == false )
				{
					PlaySound( "grunt" + Rand.Int( 1, 4 ) );
				}
			}

			LastDamageInfo = info;

			base.TakeDamage( info );
		}

		public void RemoveRagdollEntity()
		{
			if ( Ragdoll != null && Ragdoll.IsValid() )
			{
				Ragdoll.Delete();
				Ragdoll = null;
			}
		}

		[ClientRpc]
		public void DidDamage( Vector3 position, float amount, float inverseHealth )
		{
			Sound.FromScreen( "dm.ui_attacker" ).SetPitch( 1 + inverseHealth * 1 );
			HitIndicator.Current?.OnHit( position, amount );
		}

		[ClientRpc]
		public void TookDamage( Vector3 position, DamageFlags flags )
		{
			if ( flags.HasFlag( DamageFlags.Fall ) )
				return;

			DamageIndicator.Current?.OnHit( position );
		}

		protected override void OnDestroy()
		{
			ShowSenseParticles( false );
			RemoveRagdollEntity();

			if ( IsServer )
			{
				DestroyLaserDot();
			}

			base.OnDestroy();
		}
	}
}
