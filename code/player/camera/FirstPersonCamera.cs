using Sandbox;
using System.Linq;

namespace Facepunch.Hidden
{
	public partial class FirstPersonCamera : ICamera
	{
		public void Activated()
		{

		}

		public void Deactivated()
		{

		}

		public void Update()
		{
			if ( Local.Pawn is not Player player )
				return;

			Camera.Rotation = player.ViewAngles.ToRotation();
			Camera.Position = player.EyePosition;
			Camera.FieldOfView = Local.UserPreference.FieldOfView;
			Camera.FirstPersonViewer = player;
			Camera.ZNear = 1f;
			Camera.ZFar = 5000f;
		}
	}
}
