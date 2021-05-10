using Sandbox;

namespace HiddenGamemode
{
	partial class Player
	{
		public PlayerCorpse Ragdoll { get; set; }

		private void BecomeRagdollOnServer( Vector3 force, int forceBone )
		{
			var ragdoll = new PlayerCorpse
			{
				WorldPos = WorldPos,
				WorldRot = WorldRot
			};

			ragdoll.CopyFrom( this );
			ragdoll.ApplyForceToBone( force, forceBone );
			ragdoll.Player = this;

			Ragdoll = ragdoll;
		}
	}
}
