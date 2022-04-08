using Sandbox;
using System;
using System.Linq;

namespace HiddenGamemode
{
	public partial class Player : Sandbox.Player
	{
		[Net, Predicted] public float Stamina { get; set; }
		[Net, Local] public SenseAbility Sense { get; set; }
		[Net, Local] public ScreamAbility Scream { get; set; }
		[Net, Local] public DeploymentType Deployment { get; set; }

		private Rotation _lastCameraRot = Rotation.Identity;
		private DamageInfo _lastDamageInfo;
		private PhysicsBody _ragdollBody;
		public PhysicsJoint _ragdollWeld;
		private Particles _senseParticles;
		private float _walkBob = 0;
		private float _lean = 0;
		private float _FOV = 0;

		public bool HasTeam
		{
			get => Team != null;
		}

		public Player()
		{
			Inventory = new Inventory( this );
			Animator = new StandardPlayerAnimator();
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

			Stamina = 100f;

			base.Respawn();
		}

		public override void OnKilled()
		{
			base.OnKilled();

			ShowFlashlight( false, false );
			ShowSenseParticles( false );
			DrawPlayer( false );

			BecomeRagdollOnServer( _lastDamageInfo.Force, GetHitboxBone( _lastDamageInfo.HitboxIndex ) );

			Inventory.DeleteContents();

			Team?.OnPlayerKilled( this );
		}

		public override void Simulate( Client client )
		{
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
					TickPickupRagdoll();
					UpdateLaserDot();
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
			if ( _senseParticles != null )
			{
				_senseParticles.Destroy( false );
				_senseParticles = null;
			}

			if ( shouldShow )
			{
				_senseParticles = Particles.Create( "particles/sense.vpcf" );

				if ( _senseParticles != null )
					_senseParticles.SetEntity( 0, this, true );
			}
		}

		public void SwitchToBestWeapon()
		{
			var best = Children.Select( x => x as Weapon )
				.Where( x => x.IsValid() && x.IsUsable() )
				.OrderByDescending( x => x.BucketWeight )
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

			if ( _lastCameraRot == Rotation.Identity )
				_lastCameraRot = CurrentView.Rotation;

			var angleDiff = Rotation.Difference( _lastCameraRot, CurrentView.Rotation );
			var angleDiffDegrees = angleDiff.Angle();
			var allowance = 20.0f;

			if ( angleDiffDegrees > allowance )
			{
				_lastCameraRot = Rotation.Lerp( _lastCameraRot, CurrentView.Rotation, 1.0f - (allowance / angleDiffDegrees) );
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
				.HitLayer( CollisionLayer.Debris )
				.Ignore( ActiveChild )
				.Ignore( this )
				.Radius( 2 )
				.Run();

			if ( trace.Hit && trace.Entity is PlayerCorpse corpse && corpse.Player != null )
			{
				if ( _ragdollWeld != null )
				{
					_ragdollBody = trace.Body;
					_ragdollWeld = PhysicsJoint.CreateLength( PhysicsPoint.Local( PhysicsBody, Vector3.Down * 6.5f ), PhysicsPoint.World( trace.Body, trace.EndPosition ), 100 );

					return;
				}
			}

			if ( _ragdollWeld == null )
			{
				trace = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 40f )
					.HitLayer( CollisionLayer.WORLD_GEOMETRY )
					.Ignore( ActiveChild )
					.Ignore( this )
					.Radius( 2 )
					.Run();

				if ( trace.Hit && _ragdollBody != null && _ragdollBody.IsValid() )
				{
					// TODO: This should be a weld joint to the world but it doesn't work right now.
					_ragdollBody.BodyType = PhysicsBodyType.Static;
					_ragdollBody.Position = trace.EndPosition - (trace.Direction * 2.5f);

					/*
					PhysicsJoint.Weld
						.From( trace.Body, trace.Body.Transform.PointToLocal( trace.EndPos ) )
						.To( _ragdollBody, _ragdollBody.Transform.PointToLocal( trace.EndPos ) )
						.Create();
					*/
				}

				_ragdollWeld.Remove();
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
				_walkBob += Time.Delta * 25.0f * speed;
			}

			camera.Position += up * MathF.Sin( _walkBob ) * speed * 2;
			camera.Position += left * MathF.Sin( _walkBob * 0.6f ) * speed * 1;

			_lean = _lean.LerpTo( Velocity.Dot( camera.Rotation.Right ) * 0.03f, Time.Delta * 15.0f );

			var appliedLean = _lean;
			appliedLean += MathF.Sin( _walkBob ) * speed * 0.2f;
			camera.Rotation *= Rotation.From( 0, 0, appliedLean );

			speed = (speed - 0.7f).Clamp( 0, 1 ) * 3.0f;

			_FOV = _FOV.LerpTo( speed * 20 * MathF.Abs( forwardspeed ), Time.Delta * 2.0f );

			//Set to a constant because it seems to actually modify camera the camera FOV leading to wackiness if you +=
			camera.FieldOfView = 70 + _FOV;
		}

		public override void TakeDamage( DamageInfo info )
		{
			if ( info.HitboxIndex == 0 )
			{
				info.Damage *= 2.0f;
			}

			if ( info.Attacker is Player attacker && attacker != this )
			{
				if ( !Game.AllowFriendlyFire )
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

			_lastDamageInfo = info;

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
			Sound.FromScreen( "dm.ui_attacker" )
				.SetPitch( 1 + inverseHealth * 1 );

			HitIndicator.Current?.OnHit( position, amount );
		}

		[ClientRpc]
		public void TookDamage( Vector3 position, DamageFlags flags )
		{
			if ( flags.HasFlag( DamageFlags.Fall ) )
				_ = new Sandbox.ScreenShake.Perlin( 2f, 1f, 1.5f, 0.8f );

			DamageIndicator.Current?.OnHit( position );
		}

		protected override void OnDestroy()
		{
			ShowSenseParticles( false );
			RemoveRagdollEntity();
			DestroyLaserDot();

			base.OnDestroy();
		}
	}
}
