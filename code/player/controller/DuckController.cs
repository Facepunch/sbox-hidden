using Sandbox;

namespace Facepunch.Hidden
{
	[Library]
	public class DuckController : BaseNetworkable
	{
		public BasePlayerController Controller;

		public bool IsActive { get; private set; }

		public DuckController( BasePlayerController controller )
		{
			Controller = controller;
		}

		public virtual void PreTick()
		{
			bool wants = Input.Down( InputButton.Duck );

			if ( wants != IsActive )
			{
				if ( wants )
					TryDuck();
				else
					TryUnDuck();
			}

			if ( IsActive )
			{
				Controller.SetTag( "ducked" );
				Controller.EyeLocalPosition *= 0.5f;
			}
		}

		private void TryDuck()
		{
			IsActive = true;
		}

		private void TryUnDuck()
		{
			var pm = Controller.TraceBBox( Controller.Position, Controller.Position, OriginalMins, OriginalMaxs );

			if ( pm.StartedSolid )
				return;

			IsActive = false;
		}

		private Vector3 OriginalMins;
		private Vector3 OriginalMaxs;

		internal void UpdateBBox( ref Vector3 mins, ref Vector3 maxs )
		{
			OriginalMins = mins;
			OriginalMaxs = maxs;

			if ( IsActive )
				maxs = maxs.WithZ( 36 );
		}

		public float GetWishSpeed()
		{
			if ( !IsActive ) return -1f;
			return 64f;
		}
	}
}
