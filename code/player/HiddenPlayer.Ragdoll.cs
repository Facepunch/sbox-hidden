using Sandbox;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
	public partial class HiddenPlayer
	{
		public PlayerCorpse Ragdoll { get; set; }

		private void BecomeRagdollOnServer( DamageInfo info )
		{
			var ragdoll = new PlayerCorpse
			{
				Position = Position,
				Rotation = Rotation
			};

			ragdoll.CopyFrom( this );
			ragdoll.ApplyForceToBone( info.Force, LastDamageInfo.BoneIndex );
			ragdoll.Player = this;

			if ( info.HasTag( "blast" ) )
			{
				ragdoll.NumberOfFeedsLeft = 0;
				ragdoll.SetMaterialGroup( 5 );
			}

			CreateRagdollBloodPuddle( ragdoll, 3f );

			Ragdoll = ragdoll;
		}

		private async void CreateRagdollBloodPuddle( PlayerCorpse ragdoll, float delay )
		{
			await Task.DelaySeconds( delay );

			if ( !ragdoll.IsValid() ) return;

			var trace = Trace.Ray( ragdoll.WorldSpaceBounds.Center, ragdoll.WorldSpaceBounds.Center + Vector3.Down * 300f )
				.WorldOnly()
				.Run();

			if ( trace.Hit )
			{
				var pool = Particles.Create( "particles/blood/blood_puddle.vpcf", Ragdoll );
				pool.SetPosition( 0, trace.EndPosition );
			}
		}
	}
}
