
namespace Sandbox
{
	public class HiddenFirstPersonCamera : CameraMode
	{
		private Vector3 LastPosition { get; set; }

		public override void Activated()
		{
			var pawn = Local.Pawn;
			if ( pawn == null ) return;

			Position = pawn.EyePosition;
			Rotation = pawn.EyeRotation;

			LastPosition = Position;
		}

		public override void Update()
		{
			var pawn = Local.Pawn;
			if ( pawn == null ) return;

			var eyePos = pawn.EyePosition;
			if ( eyePos.Distance( LastPosition ) < 300 )
			{
				Position = Vector3.Lerp( eyePos.WithZ( LastPosition.z ), eyePos, 20f * Time.Delta );
			}
			else
			{
				Position = eyePos;
			}

			Rotation = pawn.EyeRotation;

			Viewer = pawn;
			LastPosition = Position;
		}
	}
}
