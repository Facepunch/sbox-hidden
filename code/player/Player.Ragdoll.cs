using Sandbox;

namespace HiddenGamemode
{
	partial class Player
	{
		public ModelEntity RagdollEntity { get; set; }

		private void BecomeRagdollOnServer( Vector3 force, int forceBone )
		{
			var ragdoll = new ModelEntity
			{
				Pos = Pos,
				Rot = Rot,
				MoveType = MoveType.Physics,
				UsePhysicsCollision = true
			};

			ragdoll.SetInteractsAs( CollisionLayer.Debris );
			ragdoll.SetInteractsWith( CollisionLayer.WORLD_GEOMETRY );
			ragdoll.SetInteractsExclude( CollisionLayer.Player | CollisionLayer.Debris );

			ragdoll.SetModel( GetModelName() );
			ragdoll.CopyBonesFrom( this );
			ragdoll.TakeDecalsFrom( this );
			ragdoll.SetRagdollVelocityFrom( this );

			foreach ( var child in Children )
			{
				if ( child is ModelEntity e )
				{
					var model = e.GetModelName();

					if ( model != null && !model.Contains( "clothes" ) )
						continue;

					var clothing = new ModelEntity();
					clothing.SetModel( model );
					clothing.SetParent( ragdoll, true );
				}
			}

			ragdoll.PhysicsGroup.AddVelocity( force );

			if ( forceBone >= 0 )
			{
				var body = ragdoll.GetBonePhysicsBody( forceBone );

				if ( body != null )
				{
					body.ApplyForce( force * 1000 );
				}
				else
				{
					ragdoll.PhysicsGroup.AddVelocity( force );
				}
			}

			RagdollEntity = ragdoll;
		}
	}
}
