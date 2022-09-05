using Sandbox;

namespace Facepunch.Hidden
{
	partial class Player
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
			ragdoll.ApplyForceToBone( info.Force, GetHitboxBone( LastDamageInfo.HitboxIndex ) );
			ragdoll.Player = this;

			if ( info.Flags.HasFlag( DamageFlags.Blast ) )
			{
				ragdoll.NumberOfFeedsLeft = 0;
				ragdoll.SetMaterialGroup( 5 );
			}

			Ragdoll = ragdoll;
		}
	}
}
