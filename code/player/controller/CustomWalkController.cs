using Sandbox;
using System;

namespace Facepunch.Hidden
{
	[Library]
	public class CustomWalkController : BasePlayerController
	{
		public virtual bool EnableSprinting => true;
		public virtual float SprintSpeed { get; set; } = 320.0f;
		public virtual float WalkSpeed { get; set; } = 150.0f;
		public float Acceleration { get; set; } = 10.0f;
		public float AirAcceleration { get; set; } = 50.0f;
		public float FallSoundZ { get; set; } = -30.0f;
		public float GroundFriction { get; set; } = 4.0f;
		public float StopSpeed { get; set; } = 100.0f;
		public float Size { get; set; } = 20.0f;
		public float DistEpsilon { get; set; } = 0.03125f;
		public float GroundAngle { get; set; } = 46.0f;
		public float Bounce { get; set; } = 0.0f;
		public float MoveFriction { get; set; } = 1.0f;
		public float StepSize { get; set; } = 18.0f;
		public float MaxNonJumpVelocity { get; set; } = 140.0f;
		public float BodyGirth { get; set; } = 32.0f;
		public float BodyHeight { get; set; } = 72.0f;
		public float EyeHeight { get; set; } = 64.0f;
		public float Gravity { get; set; } = 800.0f;
		public float AirControl { get; set; } = 30.0f;
		public bool Swimming { get; set; } = false;
		public bool AutoJump { get; set; } = false;

		public DuckController Duck;
		public Unstuck Unstuck;

		public CustomWalkController()
		{
			Duck = new DuckController( this );
			Unstuck = new Unstuck( this );
		}

		/// <summary>
		/// Get the hull size for the player's collision.
		/// </summary>
		public override BBox GetHull()
		{
			var girth = BodyGirth * 0.5f;
			var mins = new Vector3( -girth, -girth, 0 );
			var maxs = new Vector3( +girth, +girth, BodyHeight );

			return new BBox( mins, maxs );
		}

		protected bool IsTouchingLadder = false;
		protected Vector3 LadderNormal;
		protected float SurfaceFriction;
		protected Vector3 Mins;
		protected Vector3 Maxs;

		public virtual void SetBBox( Vector3 mins, Vector3 maxs )
		{
			if ( Mins == mins && Maxs == maxs )
				return;

			Mins = mins;
			Maxs = maxs;
		}

		/// <summary>
		/// Update the size of the bbox. We should really trigger some shit if this changes.
		/// </summary>
		public virtual void UpdateBBox()
		{
			var girth = BodyGirth * 0.5f;
			var mins = new Vector3( -girth, -girth, 0 ) * Pawn.Scale;
			var maxs = new Vector3( +girth, +girth, BodyHeight ) * Pawn.Scale;

			Duck.UpdateBBox( ref mins, ref maxs );

			SetBBox( mins, maxs );
		}

		public override void Simulate()
		{
			EyeLocalPosition = Vector3.Up * (EyeHeight * Pawn.Scale);
			UpdateBBox();

			EyeLocalPosition += TraceOffset;
			EyeRotation = Input.Rotation;

			if ( Unstuck.TestAndFix() )
				return;

			CheckLadder();
			Swimming = Pawn.WaterLevel > 0.6f;

			if ( !Swimming && !IsTouchingLadder )
			{
				Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
				Velocity += new Vector3( 0, 0, BaseVelocity.z ) * Time.Delta;

				BaseVelocity = BaseVelocity.WithZ( 0 );
			}

			HandleJumping();

			var startOnGround = GroundEntity != null;

			if ( startOnGround )
			{
				Velocity = Velocity.WithZ( 0 );

				if ( GroundEntity != null )
				{
					ApplyFriction( GroundFriction * SurfaceFriction );
				}
			}

			WishVelocity = new Vector3( Input.Forward, Input.Left, 0 );
			var inSpeed = WishVelocity.Length.Clamp( 0, 1 );
			WishVelocity *= Input.Rotation;

			if ( !Swimming && !IsTouchingLadder )
			{
				WishVelocity = WishVelocity.WithZ( 0 );
			}

			WishVelocity = WishVelocity.Normal * inSpeed;
			WishVelocity *= GetWishSpeed();

			Duck.PreTick();

			var stayOnGround = false;

			OnPreTickMove();

			if ( Swimming )
			{
				ApplyFriction( 1 );
				WaterMove();
			}
			else if ( IsTouchingLadder )
			{
				LadderMove();
			}
			else if ( GroundEntity != null )
			{
				stayOnGround = true;
				WalkMove();
			}
			else
			{
				AirMove();
			}

			CategorizePosition( stayOnGround );

			if ( !Swimming && !IsTouchingLadder )
			{
				Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
			}

			if ( GroundEntity != null )
			{
				Velocity = Velocity.WithZ( 0 );
			}
		}

		public virtual float GetWishSpeed()
		{
			var ws = Duck.GetWishSpeed();
			if ( ws >= 0 ) return ws;

			if ( EnableSprinting && Input.Down( InputButton.Run ) )
			{
				return SprintSpeed;
			}
			if ( Input.Down( InputButton.Walk ) ) return WalkSpeed/2;
			
			return WalkSpeed;
		}

		private void WalkMove()
		{
			var wishdir = WishVelocity.Normal;
			var wishspeed = WishVelocity.Length;

			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.Normal * wishspeed;

			Velocity = Velocity.WithZ( 0 );
			Accelerate( wishdir, wishspeed, 0, Acceleration );
			Velocity = Velocity.WithZ( 0 );

			Velocity += BaseVelocity;

			try
			{
				if ( Velocity.Length < 1.0f )
				{
					Velocity = Vector3.Zero;
					return;
				}

				var dest = (Position + Velocity * Time.Delta).WithZ( Position.z );
				var pm = TraceBBox( Position, dest );

				if ( pm.Fraction == 1 )
				{
					Position = pm.EndPosition;
					StayOnGround();
					return;
				}

				StepMove();
			}
			finally
			{
				Velocity -= BaseVelocity;
			}

			StayOnGround();
		}

		private void StepMove()
		{
			var startPos = Position;
			var startVel = Velocity;

			TryPlayerMove();

			var withoutStepPos = Position;
			var withoutStepVel = Velocity;

			Position = startPos;
			Velocity = startVel;

			var trace = TraceBBox( Position, Position + Vector3.Up * (StepSize + DistEpsilon) );
			if ( !trace.StartedSolid ) Position = trace.EndPosition;

			TryPlayerMove();

			trace = TraceBBox( Position, Position + Vector3.Down * (StepSize + DistEpsilon * 2) );

			if ( !trace.Hit || Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle )
			{
				Position = withoutStepPos;
				Velocity = withoutStepVel;
				return;
			}


			if ( !trace.StartedSolid )
				Position = trace.EndPosition;

			var withStepPos = Position;

			float withoutStep = (withoutStepPos - startPos).WithZ( 0 ).Length;
			float withStep = (withStepPos - startPos).WithZ( 0 ).Length;

			if ( withoutStep > withStep )
			{
				Position = withoutStepPos;
				Velocity = withoutStepVel;

				return;
			}
		}

		/// <summary>
		/// Add our wish direction and speed onto our velocity.
		/// </summary>
		public virtual void Accelerate( Vector3 wishDir, float wishSpeed, float speedLimit, float acceleration )
		{
			if ( speedLimit > 0 && wishSpeed > speedLimit )
				wishSpeed = speedLimit;

			var currentSpeed = Velocity.Dot( wishDir );
			var addSpeed = wishSpeed - currentSpeed;

			if ( addSpeed <= 0 )
				return;

			var accelSpeed = acceleration * Time.Delta * wishSpeed * SurfaceFriction;

			if ( accelSpeed > addSpeed )
				accelSpeed = addSpeed;

			Velocity += wishDir * accelSpeed;
		}

		/// <summary>
		/// Remove ground friction from velocity.
		/// </summary>
		public virtual void ApplyFriction( float frictionAmount = 1.0f )
		{
			var speed = Velocity.Length;
			if ( speed < 0.1f ) return;

			var control = (speed < StopSpeed) ? StopSpeed : speed;
			var drop = control * Time.Delta * frictionAmount;
			var newspeed = speed - drop;

			if ( newspeed < 0 ) newspeed = 0;

			if ( newspeed != speed )
			{
				newspeed /= speed;
				Velocity *= newspeed;
			}
		}

		public virtual void AirMove()
		{
			var wishdir = WishVelocity.Normal;
			var wishspeed = WishVelocity.Length;

			Accelerate( wishdir, wishspeed, AirControl, AirAcceleration );

			Velocity += BaseVelocity;

			TryPlayerMove();

			Velocity -= BaseVelocity;
		}

		public virtual void WaterMove()
		{
			var wishDir = WishVelocity.Normal;
			var wishSpeed = WishVelocity.Length;

			wishSpeed *= 0.8f;

			Accelerate( wishDir, wishSpeed, 100, Acceleration );

			Velocity += BaseVelocity;

			TryPlayerMove();

			Velocity -= BaseVelocity;
		}

		public virtual void CheckLadder()
		{
			if ( IsTouchingLadder && Input.Pressed( InputButton.Jump ) )
			{
				Velocity = LadderNormal * 100.0f;
				IsTouchingLadder = false;

				return;
			}

			var ladderDistance = 1.0f;
			var start = Position;
			var end = start + (IsTouchingLadder ? (LadderNormal * -1.0f) : WishVelocity.Normal) * ladderDistance;

			var pm = Trace.Ray( start, end )
				.Size( Mins, Maxs )
				.WithTag( "ladder" )
				.Ignore( Pawn )
				.Run();

			IsTouchingLadder = false;

			if ( pm.Hit )
			{
				IsTouchingLadder = true;
				LadderNormal = pm.Normal;
			}
		}

		public virtual void LadderMove()
		{
			var velocity = WishVelocity;
			var normalDot = velocity.Dot( LadderNormal );
			var cross = LadderNormal * normalDot;

			Velocity = (velocity - cross) + (-normalDot * LadderNormal.Cross( Vector3.Up.Cross( LadderNormal ).Normal ));

			TryPlayerMove();
		}

		public virtual void TryPlayerMove()
		{
			var mover = new MoveHelper( Position, Velocity );
			mover.Trace = mover.Trace.Size( Mins, Maxs ).Ignore( Pawn );
			mover.MaxStandableAngle = GroundAngle;
			mover.TryMove( Time.Delta );

			Position = mover.Position;
			Velocity = mover.Velocity;
		}

		/// <summary>
		/// Check for a new ground entity.
		/// </summary>
		public virtual void UpdateGroundEntity( TraceResult tr )
		{
			GroundNormal = tr.Normal;

			SurfaceFriction = tr.Surface.Friction * 1.25f;
			if ( SurfaceFriction > 1 ) SurfaceFriction = 1;

			GroundEntity = tr.Entity;

			if ( GroundEntity != null )
			{
				BaseVelocity = GroundEntity.Velocity;
			}
		}

		/// <summary>
		/// We're no longer on the ground, remove it.
		/// </summary>
		public virtual void ClearGroundEntity()
		{
			if ( GroundEntity == null ) return;

			GroundEntity = null;
			GroundNormal = Vector3.Up;
			SurfaceFriction = 1.0f;
		}

		/// <summary>
		/// Traces the current bbox and returns the result.
		/// liftFeet will move the start position up by this amount, while keeping the top of the bbox at the same 
		/// position. This is good when tracing down because you won't be tracing through the ceiling above.
		/// </summary>
		public override TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f )
		{
			return TraceBBox( start, end, Mins, Maxs, liftFeet );
		}

		/// <summary>
		/// Try to keep a walking player on the ground when running down slopes, etc.
		/// </summary>
		public virtual void StayOnGround()
		{
			var start = Position + Vector3.Up * 2;
			var end = Position + Vector3.Down * StepSize;
			var trace = TraceBBox( Position, start );

			start = trace.EndPosition;
			trace = TraceBBox( start, end );

			if ( trace.Fraction <= 0 ) return;
			if ( trace.Fraction >= 1 ) return;
			if ( trace.StartedSolid ) return;
			if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle ) return;

			Position = trace.EndPosition;
		}

		public virtual void OnPreTickMove() { }
		public virtual void AddJumpVelocity() { }

		public virtual void HandleJumping()
		{
			if ( AutoJump ? Input.Down( InputButton.Jump ) : Input.Pressed( InputButton.Jump ) )
			{
				CheckJumpButton();
			}
		}

		public virtual void OnPostCategorizePosition( bool stayOnGround, TraceResult trace ) { }

		protected void CheckJumpButton()
		{
			if ( Swimming )
			{
				ClearGroundEntity();
				Velocity = Velocity.WithZ( 100f );

				return;
			}

			if ( GroundEntity == null )
				return;

			ClearGroundEntity();

			var flGroundFactor = 1f;
			var flMul = 268.3281572999747f * 1.2f;
			var startZ = Velocity.z;

			if ( Duck.IsActive )
				flMul *= 0.8f;

			Velocity = Velocity.WithZ( startZ + flMul * flGroundFactor );
			Velocity -= new Vector3( 0f, 0f, Gravity * 0.5f ) * Time.Delta;

			AddJumpVelocity();
			AddEvent( "jump" );
		}

		private void CategorizePosition( bool stayOnGround )
		{
			SurfaceFriction = 1.0f;

			var point = Position - Vector3.Up * 2;
			var bumpOrigin = Position;
			var isMovingUpFast = Velocity.z > MaxNonJumpVelocity;
			var moveToEndPos = false;

			if ( GroundEntity != null )
			{
				moveToEndPos = true;
				point.z -= StepSize;
			}
			else if ( stayOnGround )
			{
				moveToEndPos = true;
				point.z -= StepSize;
			}

			if ( isMovingUpFast || Swimming )
			{
				ClearGroundEntity();
				return;
			}

			var pm = TraceBBox( bumpOrigin, point, 4.0f );

			if ( pm.Entity == null || Vector3.GetAngle( Vector3.Up, pm.Normal ) > GroundAngle )
			{
				ClearGroundEntity();
				moveToEndPos = false;

				if ( Velocity.z > 0 )
					SurfaceFriction = 0.25f;
			}
			else
			{
				UpdateGroundEntity( pm );
			}

			if ( moveToEndPos && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
			{
				Position = pm.EndPosition;
			}

			OnPostCategorizePosition( stayOnGround, pm );
		}
	}
}
