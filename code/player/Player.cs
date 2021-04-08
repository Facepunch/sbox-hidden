using Sandbox;
using System;
using System.Linq;

namespace HiddenGamemode
{
	public partial class Player : BasePlayer
	{
		[NetLocal] public SenseAbility Sense { get; set; }
		[NetLocal] public ScreamAbility Scream { get; set; }

		private TimeSince _timeSinceDropped;
		private RealTimeSince _timeSinceLastUpdatedFrameRate;
		private Rotation _lastCameraRot = Rotation.Identity;
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

			Team?.OnPlayerKilled( this );
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

			if ( Team != null )
			{
				Team.OnTick( this );
			}

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
			if ( _timeSinceDropped < 1 ) return;

			base.StartTouch( other );
		}

		public override void PostCameraSetup( Camera camera )
		{
			base.PostCameraSetup( camera );

			if ( _lastCameraRot == Rotation.Identity )
				_lastCameraRot = Camera.Rot;

			var angleDiff = Rotation.Difference( _lastCameraRot, Camera.Rot );
			var angleDiffDegrees = angleDiff.Angle();
			var allowance = 20.0f;

			if ( angleDiffDegrees > allowance )
			{
				_lastCameraRot = Rotation.Lerp( _lastCameraRot, Camera.Rot, 1.0f - (allowance / angleDiffDegrees) );
			}

			if ( camera is FirstPersonCamera )
			{
				AddCameraEffects( camera );
			}

			if ( _timeSinceLastUpdatedFrameRate > 1 )
			{
				_timeSinceLastUpdatedFrameRate = 0;
				UpdateFps( (int)(1.0f / Time.Delta) );
			}
		}

		private void AddCameraEffects( Camera camera )
		{
			var speed = Velocity.Length.LerpInverse( 0, 320 );
			var forwardspeed = Velocity.Normal.Dot( camera.Rot.Forward );

			var left = camera.Rot.Left;
			var up = camera.Rot.Up;

			if ( GroundEntity != null )
			{
				_walkBob += Time.Delta * 25.0f * speed;
			}

			camera.Pos += up * MathF.Sin( _walkBob ) * speed * 2;
			camera.Pos += left * MathF.Sin( _walkBob * 0.6f ) * speed * 1;

			_lean = _lean.LerpTo( Velocity.Dot( camera.Rot.Right ) * 0.03f, Time.Delta * 15.0f );

			var appliedLean = _lean;
			appliedLean += MathF.Sin( _walkBob ) * speed * 0.2f;
			camera.Rot *= Rotation.From( 0, 0, appliedLean );

			speed = (speed - 0.7f).Clamp( 0, 1 ) * 3.0f;

			_FOV = _FOV.LerpTo( speed * 20 * MathF.Abs( forwardspeed ), Time.Delta * 2.0f );

			camera.FieldOfView += _FOV;
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
				attacker.DidDamage( info.Position, info.Damage, ((float)Health).LerpInverse( 100, 0 ) );
			}

			TookDamage( info.Weapon.IsValid() ? info.Weapon.WorldPos : info.Attacker.WorldPos );
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
