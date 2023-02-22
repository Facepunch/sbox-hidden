using Gamelib.Utility;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Hidden
{
	public partial class Projectile : ModelEntity
	{
		public static T Create<T>( string dataName ) where T : Projectile, new()
		{
			var data = ResourceLibrary.GetAll<ProjectileData>()
				.FirstOrDefault( d => d.ResourceName.ToLower() == dataName.ToLower() );

			if ( data == null )
			{
				throw new Exception( $"Unable to find Projectile Data by name {dataName}" );
			}

			var projectile = new T();
			projectile.Data = data;
			return projectile;
		}

		[Net, Predicted] public ProjectileData Data { get; set; }

		public Action<Projectile, TraceResult> Callback { get; private set; }
		public List<string> FlybySounds { get; set; }
		public bool PlayFlybySounds { get; set; } = false;
		public RealTimeUntil CanHitTime { get; set; } = 0.1f;
		public ProjectileSimulator Simulator { get; set; }
		public string Attachment { get; set; } = null;
		public Entity Attacker { get; set; } = null;
		public bool ExplodeOnDestroy { get; set; } = true;
		public Entity IgnoreEntity { get; set; }
		public Vector3 StartPosition { get; private set; }
		public bool Debug { get; set; } = false;

		protected float GravityModifier { get; set; }
		protected RealTimeUntil NextFlyby { get; set; }
		protected RealTimeUntil DestroyTime { get; set; }
		protected SceneObject ModelEntity { get; set; }
		protected Vector3 InitialVelocity { get; set; }
		protected Sound LaunchSound { get; set; }
		protected Particles Follower { get; set; }
		protected Particles Trail { get; set; }
		protected float LifeTime { get; set; }
		protected float Gravity { get; set; }

		public void Initialize( Vector3 start, Vector3 velocity, Action<Projectile, TraceResult> callback = null )
		{
			LifeTime = Data.LifeTime.GetValue();
			Gravity = Data.Gravity.GetValue();

			if ( LifeTime > 0f )
			{
				DestroyTime = LifeTime;
			}

			InitialVelocity = velocity;
			StartPosition = start;
			EnableDrawing = false;
			Velocity = velocity;
			Callback = callback;
			NextFlyby = 0.2f;
			Position = start;

			if ( Data.FaceDirection )
			{
				Rotation = Rotation.LookAt( Velocity.Normal );
			}

			if ( Simulator.IsValid() )
			{
				Simulator?.Add( this );
				Owner = Simulator.Owner;

				if ( Game.IsServer )
				{
					using ( LagCompensation() )
					{
						// Work out the number of ticks for this client's latency that it took for us to receive this input.
						var tickDifference = ((float)(Owner.Client.Ping / 2000f) / Time.Delta).CeilToInt();

						// Advance the simulation by that number of ticks.
						for ( var i = 0; i < tickDifference; i++ )
						{
							if ( IsValid )
							{
								Simulate();
							}
						}
					}
				}
			}

			if ( IsClientOnly )
			{
				using ( Prediction.Off() )
				{
					CreateEffects();
				}
			}
		}

		public override void Spawn()
		{
			Predictable = true;

			base.Spawn();
		}

		public override void ClientSpawn()
		{
			// We only want to create effects if we're the server-side copy.
			if ( !IsServerSideCopy() )
			{
				CreateEffects();
			}

			base.ClientSpawn();
		}

		public virtual void CreateEffects()
		{
			if ( !string.IsNullOrEmpty( Data.TrailEffect ) )
			{
				Trail = Particles.Create( Data.TrailEffect, this );

				if ( !string.IsNullOrEmpty( Attachment ) )
					Trail.SetEntityAttachment( 0, this, Attachment );
				else
					Trail.SetEntity( 0, this );
			}

			if ( !string.IsNullOrEmpty( Data.FollowEffect ) )
			{
				Follower = Particles.Create( Data.FollowEffect, this );
			}

			if ( !string.IsNullOrEmpty( Data.LaunchSound ) )
				LaunchSound = Sound.FromWorld( Data.LaunchSound, Position );

			if ( !string.IsNullOrEmpty( Data.ModelName ) )
				ModelEntity = new SceneObject( Game.SceneWorld, Data.ModelName );
		}

		public virtual void Simulate()
		{
			if ( Data.FaceDirection )
			{
				Rotation = Rotation.LookAt( Velocity.Normal );
			}

			if ( Debug )
			{
				DebugOverlay.Sphere( Position, Data.Radius, Game.IsClient ? Color.Blue : Color.Red );
			}

			var newPosition = GetTargetPosition();

			var trace = Trace.Ray( Position, newPosition )
				.WorldAndEntities()
				.UseHitboxes()
				.WithAnyTags( "weapon", "player", "solid" )
				.WithoutTags( "trigger" )
				.Size( Data.Radius )
				.Ignore( this )
				.Ignore( IgnoreEntity )
				.Run();

			Position = trace.EndPosition;

			if ( LifeTime > 0f && DestroyTime )
			{
				if ( ExplodeOnDestroy )
				{
					PlayHitEffects( Vector3.Zero );
					Callback?.Invoke( this, trace );
				}

				Delete();

				return;
			}

			PostSimulate( trace );

			if ( HasHitTarget( trace ) )
			{
				PlayHitEffects( trace.Normal );
				Callback?.Invoke( this, trace );
				Delete();
			}
			else
			{
				if ( NextFlyby && FlybySounds != null )
				{
					Weapons.PlayFlybySounds( Attacker, trace.StartPosition, trace.EndPosition, Data.Radius, Data.Radius * 4f, FlybySounds );
					NextFlyby = Time.Delta * 2f;
				}
			}
		}

		public bool IsServerSideCopy()
		{
			return !IsClientOnly && Owner.IsValid() && Owner.IsLocalPawn;
		}

		protected virtual void PostSimulate( TraceResult trace )
		{

		}

		protected virtual bool HasHitTarget( TraceResult trace )
		{
			return (trace.Hit && CanHitTime) || trace.StartedSolid;
		}

		protected virtual Vector3 GetTargetPosition()
		{
			var newPosition = Position;
			newPosition += Velocity * Time.Delta;

			GravityModifier += Gravity;
			newPosition -= new Vector3( 0f, 0f, GravityModifier * Time.Delta );

			return newPosition;
		}

		[ClientRpc]
		protected virtual void PlayHitEffects( Vector3 normal )
		{
			if ( IsServerSideCopy() )
			{
				// We don't want to play hit effects if we're the server-side copy.
				return;
			}

			if ( !string.IsNullOrEmpty( Data.ExplosionEffect ) )
			{
				var explosion = Particles.Create( Data.ExplosionEffect );

				if ( explosion != null )
				{
					explosion.SetPosition( 0, Position );
					explosion.SetForward( 0, normal );
				}
			}

			if ( !string.IsNullOrEmpty( Data.HitSound ) )
				Sound.FromWorld( Data.HitSound, Position );
		}

		[Event.PreRender]
		protected virtual void PreRender()
		{
			if ( ModelEntity.IsValid() )
			{
				ModelEntity.Transform = Transform;
			}
		}

		[Event.Tick.Server]
		protected virtual void ServerTick()
		{
			if ( !Simulator.IsValid() )
			{
				Simulate();
			}
		}

		protected override void OnDestroy()
		{
			Simulator?.Remove( this );

			RemoveEffects();

			base.OnDestroy();
		}

		private void RemoveEffects()
		{
			ModelEntity?.Delete();
			Follower?.Destroy();
			Trail?.Destroy();
			Trail = null;
		}
	}

}
