using Sandbox;
using System;

namespace HiddenGamemode
{
	[Library]
	public class CustomWalkController : BasePlayerController
	{
		public virtual float SprintSpeed { get; set; } = 320f;
		public virtual float WalkSpeed { get; set; } = 150f;
		public virtual float DefaultSpeed { get; set; } = 190f;
		public float Acceleration { get; set; } = 10f;
		public float AirAcceleration { get; set; } = 10f;
		public float FallSoundZ { get; set; } = -30f;
		public float GroundFriction { get; set; } = 4f;
		public float StopSpeed { get; set; } = 100f;
		public float Size { get; set; } = 20f;
		public float DistEpsilon { get; set; } = 0.03125f;
		public float GroundNormalZ { get; set; } = 0.707f;
		public float Bounce { get; set; } = 0.0f;
		public float MoveFriction { get; set; } = 1f;
		public float StepSize { get; set; } = 18f;
		public float MaxNonJumpVelocity { get; set; } = 140f;
		public float BodyGirth { get; set; } = 32f;
		public float BodyHeight { get; set; } = 72f;
		public float EyeHeight { get; set; } = 64f;
		public float Gravity { get; set; } = 800f;
		public float AirControl { get; set; } = 30f;
		public bool Swimming { get; set; } = false;

		public DuckController Duck { get; set; }
		public Unstuck Unstuck { get; set; }

		public CustomWalkController()
		{
			Duck = new DuckController( this );
			Unstuck = new Unstuck( this );
		}

		private readonly Vector3[] _planes = new Vector3[5];
		private float _surfaceFriction;
		private Vector3 _mins;
		private Vector3 _maxs;

		private static Vector3 ClipVelocity( Vector3 vel, Vector3 normal, float overBounce )
		{
			var backoff = Vector3.Dot( vel, normal ) * overBounce;
			var change = normal * backoff;
			var o = vel - change;
			var adjust = Vector3.Dot( o, normal );

			if ( adjust < 1.0f )
			{
				adjust = MathF.Min( adjust, -1.0f );
				o -= normal * adjust;
			}

			return o;
		}

		public void SetBBox( Vector3 mins, Vector3 maxs )
		{
			if ( _mins == mins && _maxs == maxs )
				return;

			_mins = mins;
			_maxs = maxs;
		}

		public void UpdateBBox()
		{
			var girth = BodyGirth * 0.5f;

			var mins = new Vector3( -girth, -girth, 0 );
			var maxs = new Vector3( +girth, +girth, BodyHeight );

			Duck.UpdateBBox( ref mins, ref maxs );

			SetBBox( mins, maxs );
		}

		public float GetWishSpeed()
		{
			var ws = Duck.GetWishSpeed();

			if ( ws >= 0 ) return ws;

			if ( Input.Down( InputButton.Run ) ) return SprintSpeed;
			if ( Input.Down( InputButton.Walk ) ) return WalkSpeed;

			return DefaultSpeed;
		}

		public void Accelerate( Vector3 wishDir, float wishSpeed, float speedLimit )
		{
			if ( speedLimit > 0 && wishSpeed > speedLimit )
				wishSpeed = speedLimit;

			var currentSpeed = Velocity.Dot( wishDir );
			var addSpeed = wishSpeed - currentSpeed;

			if ( addSpeed <= 0 )
				return;

			var accelSpeed = Acceleration * Time.Delta * wishSpeed * _surfaceFriction;

			if ( accelSpeed > addSpeed )
				accelSpeed = addSpeed;

			Velocity += wishDir * accelSpeed;
		}

		public void ApplyFriction( float frictionAmount = 1.0f )
		{
			var speed = Velocity.Length;
			if ( speed < 0.1f ) return;

			var control = (speed < StopSpeed) ? StopSpeed : speed;
			var drop = control * Time.Delta * frictionAmount;
			var newSpeed = speed - drop;

			if ( newSpeed < 0 ) newSpeed = 0;

			if ( newSpeed != speed )
			{
				newSpeed /= speed;
				Velocity *= newSpeed;
			}
		}

		public void ClearGroundEntity()
		{
			if ( GroundEntity == null ) return;

			GroundEntity = null;
			GroundNormal = Vector3.Up;
			_surfaceFriction = 1.0f;
		}

		public void StayOnGround()
		{
			var start = Pos + Vector3.Up * 2;
			var end = Pos + Vector3.Down * StepSize;

			var trace = TraceBBox( Pos, start );
			start = trace.EndPos;
			trace = TraceBBox( start, end );

			if ( trace.Fraction <= 0 ) return;
			if ( trace.Fraction >= 1 ) return;
			if ( trace.StartedSolid ) return;
			if ( trace.Normal.z > GroundNormalZ ) return;

			Pos = trace.EndPos;
		}

		public override TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f )
		{
			return TraceBBox( start, end, _mins, _maxs, liftFeet );
		}

		public override BBox GetHull()
		{
			var girth = BodyGirth * 0.5f;
			var mins = new Vector3( -girth, -girth, 0 );
			var maxs = new Vector3( +girth, +girth, BodyHeight );
			return new BBox( mins, maxs );
		}

		public override void Tick()
		{
			ViewOffset = Vector3.Up * EyeHeight;

			UpdateBBox();

			ViewOffset += TraceOffset;

			RestoreGroundPos();

			if ( Unstuck.TestAndFix() )
				return;

			Swimming = Player.WaterLevel.Fraction > 0.6f;

			if ( !Swimming )
			{
				Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
				Velocity += new Vector3( 0, 0, BaseVelocity.z ) * Time.Delta;

				BaseVelocity = BaseVelocity.WithZ( 0 );
			}

			if ( Input.Pressed( InputButton.Jump ) )
			{
				CheckJumpButton();
			}

			bool bStartOnGround = GroundEntity != null;

			if ( bStartOnGround )
			{
				Velocity = Velocity.WithZ( 0 );

				if ( GroundEntity != null )
				{
					ApplyFriction( GroundFriction * _surfaceFriction );
				}
			}

			WishVelocity = new Vector3( Input.Forward, Input.Left, 0 );

			var inSpeed = WishVelocity.Length.Clamp( 0, 1 );

			WishVelocity *= Input.Rot;

			if ( !Swimming )
			{
				WishVelocity = WishVelocity.WithZ( 0 );
			}

			WishVelocity = WishVelocity.Normal * inSpeed;
			WishVelocity *= GetWishSpeed();

			Duck.PreTick();

			bool stayOnGround = false;

			OnPreTickMove();

			if ( Swimming )
			{
				ApplyFriction( 1 );
				WaterMove();
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

			if ( !Swimming )
			{
				Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
			}

			if ( GroundEntity != null )
			{
				Velocity = Velocity.WithZ( 0 );
			}

			SaveGroundPos();
		}

		protected virtual void OnPostCategorizePosition( bool stayOnGround, TraceResult trace ) { }

		protected virtual void OnNewGroundEntity( Entity entity, Vector3 velocity ) { }

		protected virtual void OnPreTickMove() { }

		protected virtual void AddJumpVelocity() { }

		private void WalkMove()
		{
			var wishdir = WishVelocity.Normal;
			var wishspeed = WishVelocity.Length;

			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.Normal * wishspeed;

			Velocity = Velocity.WithZ( 0 );

			Accelerate( wishdir, wishspeed, 0 );

			Velocity = Velocity.WithZ( 0 );
			Velocity += BaseVelocity;

			try
			{
				if ( Velocity.Length < 1.0f )
				{
					Velocity = Vector3.Zero;
					return;
				}

				var dest = (Pos + Velocity * Time.Delta).WithZ( Pos.z );
				var pm = TraceBBox( Pos, dest );

				if ( pm.Fraction == 1 )
				{
					Pos = pm.EndPos;
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
			var vecPos = Pos;
			var vecVel = Velocity;

			TryPlayerMove();

			var vecDownPos = Pos;
			var vecDownVel = Velocity;

			Pos = vecPos;
			Velocity = vecVel;

			var trace = TraceBBox( Pos, Pos + Vector3.Up * (StepSize + DistEpsilon) );

			if ( !trace.StartedSolid )
			{
				Pos = trace.EndPos;
			}

			TryPlayerMove();

			trace = TraceBBox( Pos, Pos + Vector3.Down * (StepSize + DistEpsilon * 2) );

			if ( trace.Normal.z < GroundNormalZ )
			{
				Pos = vecDownPos;
				Velocity = vecDownVel.WithZ( 0 );

				return;
			}

			if ( !trace.StartedSolid )
			{
				Pos = trace.EndPos;
			}

			var vecUpPos = Pos;
			var downDistance = (vecDownPos.x - vecPos.x) * (vecDownPos.x - vecPos.x) + (vecDownPos.y - vecPos.y) * (vecDownPos.y - vecPos.y);
			var upDistance = (vecUpPos.x - vecPos.x) * (vecUpPos.x - vecPos.x) + (vecUpPos.y - vecPos.y) * (vecUpPos.y - vecPos.y);

			if ( downDistance > upDistance )
			{
				Pos = vecDownPos;
				Velocity = vecDownVel;
			}
			else
			{
				Velocity = Velocity.WithZ( vecDownVel.z );
			}
		}

		private void CheckJumpButton()
		{
			if ( Swimming )
			{
				ClearGroundEntity();
				Velocity = Velocity.WithZ( 100 );

				return;
			}

			if ( GroundEntity == null )
				return;

			ClearGroundEntity();

			var groundFactor = 1.0f;
			var multiplier = 268.3281572999747f * 1.2f;
			var startZ = Velocity.z;

			if ( Duck.IsActive )
				multiplier *= 0.8f;

			Velocity = Velocity.WithZ( startZ + multiplier * groundFactor );
			Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

			AddJumpVelocity();

			AddEvent( "jump" );
		}

		private void AirMove()
		{
			var wishDir = WishVelocity.Normal;
			var wishSpeed = WishVelocity.Length;

			Accelerate( wishDir, wishSpeed, AirControl );

			Velocity += BaseVelocity;

			TryPlayerMove();

			Velocity -= BaseVelocity;
		}

		private void WaterMove()
		{
			var wishDir = WishVelocity.Normal;
			var wishSpeed = WishVelocity.Length;

			wishSpeed *= 0.8f;

			Accelerate( wishDir, wishSpeed, 100 );

			Velocity += BaseVelocity;

			TryPlayerMove();

			Velocity -= BaseVelocity;
		}

		private void TryPlayerMove()
		{
			var timeLeft = Time.Delta;
			var allFraction = 0.0f;

			var originalVelocity = Velocity;
			var primalVelocity = Velocity;

			var numPlanes = 0;

			for ( int bumpCount = 0; bumpCount < 4; bumpCount++ )
			{
				if ( Velocity.Length == 0.0f )
					break;

				var end = Pos + Velocity * timeLeft;
				var pm = TraceBBox( Pos, end );

				allFraction += pm.Fraction;

				if ( pm.Fraction > DistEpsilon )
				{
					Pos = pm.EndPos;
					originalVelocity = Velocity;
					numPlanes = 0;
				}

				if ( pm.Fraction == 1 )
				{
					break;
				}

				var probablyFloor = pm.Normal.z > GroundNormalZ;

				timeLeft -= timeLeft * pm.Fraction;

				if ( numPlanes >= _planes.Length )
				{
					Velocity = Vector3.Zero;
					break;
				}

				_planes[numPlanes] = pm.Normal;
				numPlanes++;

				if ( numPlanes == 1 && GroundEntity == null )
				{
					var overbounce = 1.0f;

					if ( !probablyFloor )
						overbounce = 1.0f + Bounce * (1.0f - _surfaceFriction);

					originalVelocity = ClipVelocity( originalVelocity, _planes[0], overbounce );
					Velocity = originalVelocity;
				}
				else
				{
					int i;

					for ( i = 0; i < numPlanes; i++ )
					{
						Velocity = ClipVelocity( originalVelocity, _planes[i], 1 );

						int j;

						for ( j = 0; j < numPlanes; j++ )
						{
							if ( j == i ) continue;

							if ( Velocity.Dot( _planes[j] ) < 0 )
								break;
						}

						if ( j == numPlanes )
							break;
					}

					if ( i == numPlanes )
					{
						if ( numPlanes != 2 )
						{
							Velocity = Vector3.Zero;
							break;
						}

						var direction = Vector3.Cross( _planes[0], _planes[1] ).Normal;
						var dot = direction.Dot( Velocity );

						Velocity = direction * dot;
					}

					if ( Vector3.Dot( Velocity, primalVelocity ) <= 0 )
					{
						Velocity = Vector3.Zero;
						break;
					}
				}
			}

			if ( allFraction == 0 )
			{
				Velocity = Vector3.Zero;
			}
		}

		private void CategorizePosition( bool stayOnGround )
		{
			_surfaceFriction = 1.0f;

			var point = Pos - Vector3.Up * 2;
			var bumpOrigin = Pos;
			var movingUpRapidly = Velocity.z > MaxNonJumpVelocity;
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

			if ( movingUpRapidly || Swimming )
			{
				ClearGroundEntity();
				return;
			}

			var hasGroundEntity = (GroundEntity != null && GroundEntity.IsValid());
			var pm = TraceBBox( bumpOrigin, point, 4.0f );

			if ( pm.Entity == null || pm.Normal.z < GroundNormalZ )
			{
				ClearGroundEntity();
				moveToEndPos = false;

				if ( Velocity.z > 0 )
					_surfaceFriction = 0.25f;
			}
			else
			{
				UpdateGroundEntity( pm );

				if ( !hasGroundEntity && GroundEntity != null && GroundEntity.IsValid() )
				{
					OnNewGroundEntity( GroundEntity, Velocity );
				}
			}

			if ( moveToEndPos && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
			{
				Pos = pm.EndPos;
			}

			OnPostCategorizePosition( stayOnGround, pm );
		}

		public void UpdateGroundEntity( TraceResult tr )
		{
			GroundNormal = tr.Normal;

			_surfaceFriction = tr.Surface.Friction * 1.25f;

			if ( _surfaceFriction > 1 ) _surfaceFriction = 1;

			GroundEntity = tr.Entity;

			if ( GroundEntity != null )
				BaseVelocity = GroundEntity.Velocity;
		}

		private void RestoreGroundPos()
		{
			if ( GroundEntity == null || GroundEntity.IsWorld )
				return;
		}

		private void SaveGroundPos()
		{
			if ( GroundEntity == null || GroundEntity.IsWorld )
				return;

			//GroundTransform = GroundEntity.Transform.ToLocal( new Transform( Pos, Rot ) );
		}
	}
}
