using Sandbox;

namespace Facepunch.Hidden
{
	[GameResource( "Radio Command", "radio", "A radio command used by I.R.I.S." )]
	public class RadioCommandResource : GameResource
	{
		[Property] public string Text { get; set; }
		[Property] public SoundEvent Sound { get; set; }
		[Property] public SoundEvent ProximitySound { get; set; }
		[Property] public float ProximityDistance { get; set; } = 1024f;
	}
}
