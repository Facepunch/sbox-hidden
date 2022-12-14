using Sandbox;

namespace Facepunch.Hidden
{
	[Library("flashlight")]
	public partial class Flashlight : SpotLightEntity
	{
		private bool DidPlayFlickerSound;

		public Flashlight() : base()
		{
			Transmit = TransmitType.Always;
			InnerConeAngle = 10f;
			OuterConeAngle = 20f;
			Brightness = 1.2f;
			QuadraticAttenuation = 1f;
			LinearAttenuation = 0f;
			Color = new Color( 0.9f, 0.87f, 0.6f );
			Falloff = 4f;
			Enabled = true;
			DynamicShadows = true;
			Range = 1024f;
		}

		public void Reset()
		{
			DidPlayFlickerSound = false;
		}

		public bool UpdateFromBattery( float battery )
		{
			Brightness = 0.01f + ((0.69f / 100f) * battery);
			Flicker = (battery <= 10);

			if ( Game.IsServer && Flicker && !DidPlayFlickerSound )
			{
				DidPlayFlickerSound = true;
				
				var sound = PlaySound( "flashlight-flicker" );
				sound.SetVolume( 0.5f );
			}

			return (battery <= 0f);
		}
	}
}
