using Sandbox;
using Sandbox.Utility;

namespace Facepunch.Hidden;

public partial class ScreenShake
{
	public class Random : ScreenShake
	{
		public float Progress => Easing.EaseOut( ((float)LifeTime).LerpInverse( 0, Length, true ) );

		private float Length { get; set; } = 5f;
		private float Size { get; set; } = 1f;
		private TimeSince LifeTime { get; set; } = 0f;

		public Random( float length = 1.5f, float size = 1f )
		{
			Length = length;
			Size = size;
		}

		public override bool Update()
		{
			var random = Vector3.Random;
			random.z = 0f;
			random = random.Normal;

			Camera.Position += (Camera.Rotation.Right * random.x + Camera.Rotation.Up * random.y) * (1f - Progress) * Size;

			return LifeTime < Length;
		}
	}
}
