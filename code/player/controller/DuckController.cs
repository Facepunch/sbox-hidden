using Sandbox;

namespace Facepunch.Hidden
{
	public class DuckController 
	{
		public MoveController Controller { get; private set; }
		public HiddenPlayer Player => Controller.Player;
		public bool IsActive { get; private set; }

		private float Scale { get; set; } = 1f;

		public DuckController( MoveController controller )
		{
			Controller = controller;
		}

		public void PreTick()
		{
			bool wants = Input.Down( "duck" );

			if ( wants != IsActive )
			{
				if ( wants )
					TryDuck();
				else
					TryUnDuck();
			}

			var targetScale = 1f;

			if ( IsActive )
			{
				Controller.SetTag( "ducked" );

				targetScale = 0.5f;

				var lookDownDot = Player.EyeRotation.Forward.Dot( Vector3.Down );

				if ( lookDownDot > 0.5f )
				{
					var f = lookDownDot - 0.5f;
					targetScale += f.Remap( 0f, 0.5f, 0f, 0.1f );
				}
			}

			Scale = Scale.LerpTo( targetScale, Time.Delta * 8f );
			Player.EyeLocalPosition *= Scale;
		}

		private void TryDuck()
		{
			IsActive = true;
		}

		private void TryUnDuck()
		{
			var pm = Controller.TraceBBox( Player.Position, Player.Position, OriginalMins, OriginalMaxs );

			if ( pm.StartedSolid )
				return;

			IsActive = false;
		}

		private Vector3 OriginalMins { get; set; }
		private Vector3 OriginalMaxs { get; set; }

		internal void UpdateBBox( ref Vector3 mins, ref Vector3 maxs )
		{
			OriginalMins = mins;
			OriginalMaxs = maxs;

			if ( IsActive )
			{
				maxs = maxs.WithZ( 36f );
			}
		}

		public float GetWishSpeed()
		{
			if ( !IsActive )
				return -1f;

			return 64f;
		}
	}
}
