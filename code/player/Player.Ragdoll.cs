using Sandbox;

namespace HiddenGamemode
{
	partial class Player
	{
		[Net] public PlayerCorpse Ragdoll { get; set; }

		private void BecomeRagdollOnServer( Vector3 force, int forceBone )
		{
			var ragdoll = new PlayerCorpse
			{
				Pos = Pos,
				Rot = Rot
			};

			ragdoll.CopyFrom( this );
			ragdoll.ApplyForceToBone( force, forceBone );
			ragdoll.Player = this;

			Ragdoll = ragdoll;
		}
	}
}
