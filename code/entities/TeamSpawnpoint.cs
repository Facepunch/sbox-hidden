using Sandbox;
using SandboxEditor;

namespace Facepunch.Hidden
{
	[Description( "A spawnpoint for a specific team. These will always be used first." )]
	[HammerEntity, EditorModel( "models/editor/playerstart.vmdl", FixedBounds = true )]
	[Title( "Team Spawnpoint" ), Category( "Player" ), Icon( "place" )]
	public partial class TeamSpawnpoint : Entity
	{
		public enum TeamType
		{
			IRIS,
			Hidden
		}

		[Property] public TeamType Team { get; set; }
	}
}
