using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	public class AnimatorWithLegs : StandardPlayerAnimator
	{
		public SceneModel AnimatedLegs => (Pawn as Player)?.AnimatedLegs;

		private HashSet<string> IgnoredLegParams = new HashSet<string>
		{
			"b_shuffle",
			"aim_body"
		};

		public override void SetAnimParameter( string name, Vector3 val )
		{
			AnimPawn?.SetAnimParameter( name, val );

			if ( !IgnoredLegParams.Contains( name ) )
				AnimatedLegs?.SetAnimParameter( name, val );
		}

		public override void SetAnimParameter( string name, float val )
		{
			AnimPawn?.SetAnimParameter( name, val );

			if ( !IgnoredLegParams.Contains( name ) )
				AnimatedLegs?.SetAnimParameter( name, val );
		}

		public override void SetAnimParameter( string name, bool val )
		{
			AnimPawn?.SetAnimParameter( name, val );

			if ( !IgnoredLegParams.Contains( name ) )
				AnimatedLegs?.SetAnimParameter( name, val );
		}

		public override void SetAnimParameter( string name, int val )
		{
			AnimPawn?.SetAnimParameter( name, val );

			if ( !IgnoredLegParams.Contains( name ) )
				AnimatedLegs?.SetAnimParameter( name, val );
		}

		public override void ResetParameters()
		{
			AnimPawn?.ResetAnimParameters();
		}
	}
}

