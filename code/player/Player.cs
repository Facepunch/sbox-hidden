using Sandbox;
using System;
using System.Linq;

namespace HiddenGamemode
{
	public partial class Player : BasePlayer
	{
		TimeSince timeSinceDropped;

		public Player()
		{
			Inventory = new Inventory( this );
			Animator = new StandardPlayerAnimator();
		}

		public void Hide()
		{
			Controller = null;
			Camera = new FixedCamera();

			EnableAllCollisions = false;
			EnableDrawing = false;
		}

		public override void Respawn()
		{
			Game.Instance?.Round?.OnPlayerSpawn( this );

			base.Respawn();
		}

		public override void OnKilled()
		{
			base.OnKilled();

			BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex ) );

			Inventory.DeleteContents();
		}

		protected override void Tick()
		{
			base.Tick();

			if ( Input.ActiveChild != null )
			{
				ActiveChild = Input.ActiveChild;
			}

			if ( LifeState != LifeState.Alive )
			{
				return;
			}

			TickPlayerUse();

			if ( ActiveChild is Weapon weapon && !weapon.IsUsable() && weapon.TimeSincePrimaryAttack > 0.5f && weapon.TimeSinceSecondaryAttack > 0.5f )
			{
				SwitchToBestWeapon();
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

		public override void StartTouch( Entity other )
		{
			if ( timeSinceDropped < 1 ) return;

			base.StartTouch( other );
		}

		RealTimeSince timeSinceUpdatedFramerate;

		Rotation lastCameraRot = Rotation.Identity;

		public override void PostCameraSetup( Camera camera )
		{
			base.PostCameraSetup( camera );

			if ( lastCameraRot == Rotation.Identity )
				lastCameraRot = Camera.Rot;

			var angleDiff = Rotation.Difference( lastCameraRot, Camera.Rot );
			var angleDiffDegrees = angleDiff.Angle();
			var allowance = 20.0f;

			if ( angleDiffDegrees > allowance )
			{
				lastCameraRot = Rotation.Lerp( lastCameraRot, Camera.Rot, 1.0f - (allowance / angleDiffDegrees) );
			}

			if ( camera is FirstPersonCamera )
			{
				AddCameraEffects( camera );
			}

			if ( timeSinceUpdatedFramerate > 1 )
			{
				timeSinceUpdatedFramerate = 0;
				UpdateFps( (int)(1.0f / Time.Delta) );
			}
		}

		float walkBob = 0;
		float lean = 0;
		float fov = 0;

		private void AddCameraEffects( Camera camera )
		{
			var speed = Velocity.Length.LerpInverse( 0, 320 );
			var forwardspeed = Velocity.Normal.Dot( camera.Rot.Forward );

			var left = camera.Rot.Left;
			var up = camera.Rot.Up;

			if ( GroundEntity != null )
			{
				walkBob += Time.Delta * 25.0f * speed;
			}

			camera.Pos += up * MathF.Sin( walkBob ) * speed * 2;
			camera.Pos += left * MathF.Sin( walkBob * 0.6f ) * speed * 1;

			lean = lean.LerpTo( Velocity.Dot( camera.Rot.Right ) * 0.03f, Time.Delta * 15.0f );

			var appliedLean = lean;
			appliedLean += MathF.Sin( walkBob ) * speed * 0.2f;
			camera.Rot *= Rotation.From( 0, 0, appliedLean );

			speed = (speed - 0.7f).Clamp( 0, 1 ) * 3.0f;

			fov = fov.LerpTo( speed * 20 * MathF.Abs( forwardspeed ), Time.Delta * 2.0f );

			camera.FieldOfView += fov;
		}

		[OwnerRpc]
		protected void UpdateFps( int fps )
		{
			Log.Info( $"{Host.Name} OwnerRPC - UpdateFPS" );
			SetScore( "fps", fps );
		}

		DamageInfo LastDamage;

		public override void TakeDamage( DamageInfo info )
		{
			LastDamage = info;

			if ( info.HitboxIndex == 0 )
			{
				info.Damage *= 2.0f;
			}

			base.TakeDamage( info );

			if ( info.Attacker is Player attacker && attacker != this )
			{
				// Note - sending this only to the attacker!
				attacker.DidDamage( attacker, info.Position, info.Damage, ((float)Health).LerpInverse( 100, 0 ) );
			}

			TookDamage( this, info.Weapon.IsValid() ? info.Weapon.WorldPos : info.Attacker.WorldPos );
		}

		[ClientRpc]
		public void DidDamage( Vector3 pos, float amount, float healthinv )
		{
			Sound.FromScreen( "dm.ui_attacker" )
				.SetPitch( 1 + healthinv * 1 );

			HitIndicator.Current?.OnHit( pos, amount );
		}

		[ClientRpc]
		public void TookDamage( Vector3 pos )
		{
			//DebugOverlay.Sphere( pos, 5.0f, Color.Red, false, 50.0f );
			DamageIndicator.Current?.OnHit( pos );
		}
	}
}
