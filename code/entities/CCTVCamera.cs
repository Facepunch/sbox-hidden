using Sandbox;
using Editor;

namespace Facepunch.Hidden
{
	[HammerEntity]
	[Title( "CCTV Camera" )]
	[Description( "Players can spectate from this camera when they die." )]
	[EditorModel( "models/editor/camera.vmdl" )]
	public partial class CCTVCamera : ModelEntity
	{
		[Net, Property] public string AreaName { get; set; }

		public CCTVCamera()
		{
			EnableDrawing = false;
			Transmit = TransmitType.Always;
		}
	}
}
