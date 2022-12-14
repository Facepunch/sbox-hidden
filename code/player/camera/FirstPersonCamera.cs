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
			if ( Game.LocalPawn is not HiddenPlayer player )
				return;

			Camera.Rotation = player.ViewAngles.ToRotation();
			Camera.Position = player.EyePosition;
			Camera.FieldOfView = Game.Preferences.FieldOfView;
			Camera.FirstPersonViewer = player;
			Camera.ZNear = 1f;
			Camera.ZFar = 5000f;
		}
	}
}
