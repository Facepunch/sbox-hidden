using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	public class MoveController
	{
		public virtual bool EnableSprinting => true;
		public virtual float SprintSpeed { get; set; } = 320f;
		public virtual float WalkSpeed { get; set; } = 150f;
		public float Acceleration { get; set; } = 10f;
		public float AirAcceleration { get; set; } = 50f;
		public float FallSoundZ { get; set; } = -30f;
		public float GroundFriction { get; set; } = 4f;
		public float StopSpeed { get; set; } = 100f;
		public float Size { get; set; } = 20f;
		public float DistEpsilon { get; set; } = 0.03125f;
		public float GroundAngle { get; set; } = 46f;
		public float Bounce { get; set; } = 0f;
		public float MoveFriction { get; set; } = 1f;
		public float StepSize { get; set; } = 18f;
		public float MaxNonJumpVelocity { get; set; } = 140f;
		public float BodyGirth { get; set; } = 32f;
		public float BodyHeight { get; set; } = 72f;
		public float EyeHeight { get; set; } = 64f;
		public float Gravity { get; set; } = 800f;
		public float AirControl { get; set; } = 30f;
		public bool Swimming { get; set; } = false;
		public bool AutoJump { get; set; } = false;

		protected float SurfaceFriction { get; set; }
		protected bool IsTouchingLadder { get; set; }
		protected Vector3 LadderNormal { get; set; }
		protected Vector3 TraceOffset { get; set; }
		protected Vector3 PreVelocity { get; set; }
		protected Vector3 Mins { get; set; }
		protected Vector3 Maxs { get; set; }

		protected HashSet<string> Events { get; set; } = new();
		protected HashSet<string> Tags { get; set; } = new();

		public Vector3 GroundNormal { get; set; }
		public Vector3 WishVelocity { get; set; }

		public Player Player { get; private set; }
		public DuckController Duck { get; private set; }

		public bool IsServer => Host.IsServer;
		public bool IsClient => Host.IsClient;

		private int StuckTries { get; set; } = 0;

		public MoveController( Player player )
		{
			Player = player; 
			Duck = new DuckController( this );
		}

		public void ClearGroundEntity()
		{
			if ( Player.GroundEntity == null )
				return;

			Player.GroundEntity = null;
			GroundNormal = Vector3.Up;
			SurfaceFriction = 1f;
		}

		public bool HasEvent( string eventName )
		{
			if ( Events == null ) return false;
			return Events.Contains( eventName );
		}

		public bool HasTag( string tagName )
		{
			if ( Tags == null ) return false;
			return Tags.Contains( tagName );
		}

		public void AddEvent( string eventName )
		{
			if ( Events == null )
				Events = new HashSet<string>();

			if ( Events.Contains( eventName ) )
				return;

			Events.Add( eventName );
		}

		public void SetTag( string tagName )
		{
			Tags ??= new HashSet<string>();

			if ( Tags.Contains( tagName ) )
				return;

			Tags.Add( tagName );
		}

		public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f )
		{
			if ( liftFeet > 0 )
			{
				start += Vector3.Up * liftFeet;
				maxs = maxs.WithZ( maxs.z - liftFeet );
			}

			var tr = Trace.Ray( start + TraceOffset, end + TraceOffset )
				.Size( mins, maxs )
				.WithAnyTags( "solid", "playerclip", "passbullets", "player" )
				.Ignore( Player )
				.Run();

			tr.EndPosition -= TraceOffset;
			return tr;
		}

		public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0f )
		{
			return TraceBBox( start, end, Mins, Maxs, liftFeet );
		}

		public virtual BBox GetHull()
		{
			var girth = BodyGirth * 0.5f;
			var mins = new Vector3( -girth, -girth, 0 );
			var maxs = new Vector3( +girth, +girth, BodyHeight );
			return new BBox( mins, maxs );
		}

		public virtual void FrameSimulate()
		{
			Player.EyeRotation = Player.ViewAngles.ToRotation();
		}

		public virtual void Simulate()
		{
			Events?.Clear();
			Tags?.Clear();

			Player.EyeLocalPosition = Vector3.Up * (EyeHeight * Player.Scale);
			UpdateBBox();

			Player.EyeLocalPosition += TraceOffset;

			// If we're a bot, spin us around 180 degrees.
			if ( Player.Client.IsBot )
				Player.EyeRotation = Player.ViewAngles.WithYaw( Player.ViewAngles.yaw + 180f ).ToRotation();
			else
				Player.EyeRotation = Player.ViewAngles.ToRotation();

			if ( CheckStuckAndFix() )
				return;

			CheckLadder();
			Swimming = Player.WaterLevel > 0.6f;

			if ( !Swimming && !IsTouchingLadder )
			{
				Player.Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
				Player.Velocity += new Vector3( 0, 0, Player.BaseVelocity.z ) * Time.Delta;
				Player.BaseVelocity = Player.BaseVelocity.WithZ( 0 );
			}

			HandleJumping();

			var startOnGround = Player.GroundEntity.IsValid();

			if ( startOnGround )
			{
				Player.Velocity = Player.Velocity.WithZ( 0 );

				if ( Player.GroundEntity.IsValid() )
				{
					ApplyFriction( GroundFriction * SurfaceFriction );
				}
			}

			WishVelocity = new Vector3( Player.InputDirection.x, Player.InputDirection.y, 0 );
			var inSpeed = WishVelocity.Length.Clamp( 0, 1 );
			WishVelocity *= Player.EyeRotation;

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
			else if ( Player.GroundEntity.IsValid() )
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
				Player.Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
			}

			if ( Player.GroundEntity.IsValid() )
			{
				Player.Velocity = Player.Velocity.WithZ( 0 );
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
			if ( Input.Down( InputButton.Walk ) )
				return WalkSpeed * 0.5f;
			
			return WalkSpeed;
		}

		private void WalkMove()
		{
			var wishdir = WishVelocity.Normal;
			var wishspeed = WishVelocity.Length;

			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.Normal * wishspeed;

			Player.Velocity = Player.Velocity.WithZ( 0 );
			Accelerate( wishdir, wishspeed, 0, Acceleration );
			Player.Velocity = Player.Velocity.WithZ( 0 );
			Player.Velocity += Player.BaseVelocity;

			try
			{
				if ( Player.Velocity.Length < 1.0f )
				{
					Player.Velocity = Vector3.Zero;
					return;
				}

				var dest = (Player.Position + Player.Velocity * Time.Delta).WithZ( Player.Position.z );
				var pm = TraceBBox( Player.Position, dest );

				if ( pm.Fraction == 1 )
				{
					Player.Position = pm.EndPosition;
					StayOnGround();
					return;
				}

				StepMove();
			}
			finally
			{
				Player.Velocity -= Player.BaseVelocity;
			}

			StayOnGround();
		}

		private void SetBBox( Vector3 mins, Vector3 maxs )
		{
			if ( Mins == mins && Maxs == maxs )
				return;

			Mins = mins;
			Maxs = maxs;
		}

		private void UpdateBBox()
		{
			var girth = BodyGirth * 0.5f;
			var mins = new Vector3( -girth, -girth, 0 ) * Player.Scale;
			var maxs = new Vector3( +girth, +girth, BodyHeight ) * Player.Scale;

			Duck.UpdateBBox( ref mins, ref maxs );

			SetBBox( mins, maxs );
		}

		private void StepMove()
		{
			var startPos = Player.Position;
			var startVel = Player.Velocity;

			TryPlayerMove();

			var withoutStepPos = Player.Position;
			var withoutStepVel = Player.Velocity;

			Player.Position = startPos;
			Player.Velocity = startVel;

			var trace = TraceBBox( Player.Position, Player.Position + Vector3.Up * (StepSize + DistEpsilon) );
			if ( !trace.StartedSolid ) Player.Position = trace.EndPosition;

			TryPlayerMove();

			trace = TraceBBox( Player.Position, Player.Position + Vector3.Down * (StepSize + DistEpsilon * 2) );

			if ( !trace.Hit || Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle )
			{
				Player.Position = withoutStepPos;
				Player.Velocity = withoutStepVel;
				return;
			}


			if ( !trace.StartedSolid )
				Player.Position = trace.EndPosition;

			var withStepPos = Player.Position;

			float withoutStep = (withoutStepPos - startPos).WithZ( 0 ).Length;
			float withStep = (withStepPos - startPos).WithZ( 0 ).Length;

			if ( withoutStep > withStep )
			{
				Player.Position = withoutStepPos;
				Player.Velocity = withoutStepVel;

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

			var currentSpeed = Player.Velocity.Dot( wishDir );
			var addSpeed = wishSpeed - currentSpeed;

			if ( addSpeed <= 0 )
				return;

			var accelSpeed = acceleration * Time.Delta * wishSpeed * SurfaceFriction;

			if ( accelSpeed > addSpeed )
				accelSpeed = addSpeed;

			Player.Velocity += wishDir * accelSpeed;
		}

		/// <summary>
		/// Remove ground friction from velocity.
		/// </summary>
		public virtual void ApplyFriction( float frictionAmount = 1.0f )
		{
			var speed = Player.Velocity.Length;
			if ( speed < 0.1f ) return;

			var control = (speed < StopSpeed) ? StopSpeed : speed;
			var dropAmount = control * Time.Delta * frictionAmount;
			var newSpeed = speed - dropAmount;

			if ( newSpeed < 0 ) newSpeed = 0;

			if ( newSpeed != speed )
			{
				newSpeed /= speed;
				Player.Velocity *= newSpeed;
			}
		}

		public virtual void AirMove()
		{
			var wishdir = WishVelocity.Normal;
			var wishspeed = WishVelocity.Length;

			Accelerate( wishdir, wishspeed, AirControl, AirAcceleration );

			Player.Velocity += Player.BaseVelocity;

			TryPlayerMove();

			Player.Velocity -= Player.BaseVelocity;
		}

		public virtual void WaterMove()
		{
			var wishDir = WishVelocity.Normal;
			var wishSpeed = WishVelocity.Length;

			wishSpeed *= 0.8f;

			Accelerate( wishDir, wishSpeed, 100, Acceleration );

			Player.Velocity += Player.BaseVelocity;

			TryPlayerMove();

			Player.Velocity -= Player.BaseVelocity;
		}

		public virtual void CheckLadder()
		{
			if ( IsTouchingLadder && Input.Pressed( InputButton.Jump ) )
			{
				Player.Velocity = LadderNormal * 100.0f;
				IsTouchingLadder = false;

				return;
			}

			var ladderDistance = 1.0f;
			var start = Player.Position;
			var end = start + (IsTouchingLadder ? (LadderNormal * -1.0f) : WishVelocity.Normal) * ladderDistance;

			var pm = Trace.Ray( start, end )
				.Size( Mins, Maxs )
				.WithTag( "ladder" )
				.Ignore( Player )
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

			Player.Velocity = (velocity - cross) + (-normalDot * LadderNormal.Cross( Vector3.Up.Cross( LadderNormal ).Normal ));

			TryPlayerMove();
		}

		public virtual void TryPlayerMove()
		{
			var mover = new MoveHelper( Player.Position, Player.Velocity );
			mover.Trace = mover.Trace.Size( Mins, Maxs ).Ignore( Player );
			mover.MaxStandableAngle = GroundAngle;
			mover.TryMove( Time.Delta );

			Player.Position = mover.Position;
			Player.Velocity = mover.Velocity;
		}

		/// <summary>
		/// Check for a new ground entity.
		/// </summary>
		public virtual void UpdateGroundEntity( TraceResult tr )
		{
			GroundNormal = tr.Normal;

			SurfaceFriction = tr.Surface.Friction * 1.25f;
			if ( SurfaceFriction > 1 ) SurfaceFriction = 1;

			Player.GroundEntity = tr.Entity;

			if ( Player.GroundEntity.IsValid() )
			{
				Player.BaseVelocity = Player.GroundEntity.Velocity;
			}
		}

		/// <summary>
		/// Try to keep a walking player on the ground when running down slopes, etc.
		/// </summary>
		public virtual void StayOnGround()
		{
			var start = Player.Position + Vector3.Up * 2;
			var end = Player.Position + Vector3.Down * StepSize;
			var trace = TraceBBox( Player.Position, start );

			start = trace.EndPosition;
			trace = TraceBBox( start, end );

			if ( trace.Fraction <= 0 ) return;
			if ( trace.Fraction >= 1 ) return;
			if ( trace.StartedSolid ) return;
			if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle ) return;

			Player.Position = trace.EndPosition;
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
				Player.Velocity = Player.Velocity.WithZ( 100f );

				return;
			}

			if ( !Player.GroundEntity.IsValid() )
				return;

			ClearGroundEntity();

			var flGroundFactor = 1f;
			var flMul = 268.3281572999747f * 1.2f;
			var startZ = Player.Velocity.z;

			if ( Duck.IsActive )
				flMul *= 0.8f;

			Player.Velocity = Player.Velocity.WithZ( startZ + flMul * flGroundFactor );
			Player.Velocity -= new Vector3( 0f, 0f, Gravity * 0.5f ) * Time.Delta;

			AddJumpVelocity();
			AddEvent( "jump" );
		}

		private bool CheckStuckAndFix()
		{
			var result = TraceBBox( Player.Position, Player.Position );

			if ( !result.StartedSolid )
			{
				StuckTries = 0;
				return false;
			}

			if ( IsClient ) return true;

			var attemptsPerTick = 20;

			for ( int i = 0; i < attemptsPerTick; i++ )
			{
				var pos = Player.Position + Vector3.Random.Normal * (StuckTries / 2.0f);

				if ( i == 0 )
				{
					pos = Player.Position + Vector3.Up * 5;
				}

				result = TraceBBox( pos, pos );

				if ( !result.StartedSolid )
				{
					Player.Position = pos;
					return false;
				}
			}

			StuckTries++;
			return true;
		}

		private void CategorizePosition( bool stayOnGround )
		{
			SurfaceFriction = 1.0f;

			var point = Player.Position - Vector3.Up * 2;
			var bumpOrigin = Player.Position;
			var isMovingUpFast = Player.Velocity.z > MaxNonJumpVelocity;
			var moveToEndPos = false;

			if ( Player.GroundEntity.IsValid() )
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

				if ( Player.Velocity.z > 0 )
					SurfaceFriction = 0.25f;
			}
			else
			{
				UpdateGroundEntity( pm );
			}

			if ( moveToEndPos && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
			{
				Player.Position = pm.EndPosition;
			}

			OnPostCategorizePosition( stayOnGround, pm );
		}
	}
}
