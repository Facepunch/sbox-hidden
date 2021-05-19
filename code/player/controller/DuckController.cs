using Sandbox;

namespace HiddenGamemode
{
	[Library]
	public class DuckController : NetworkClass
	{
		public BasePlayerController Controller;

		public bool IsActive;

		public DuckController( BasePlayerController controller )
		{
			Controller = controller;
		}

		public virtual void PreTick()
		{
			bool wants = Controller.Input.Down( InputButton.Duck );

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
				Controller.EyePosLocal *= 0.5f;
			}
		}

		void TryDuck()
		{
			IsActive = true;
		}

		void TryUnDuck()
		{
			var pm = Controller.TraceBBox( Controller.Position, Controller.Position, _originalMins, _originalMaxs );

			if ( pm.StartedSolid )
				return;

			IsActive = false;
		}

		private Vector3 _originalMins;
		private Vector3 _originalMaxs;

		internal void UpdateBBox( ref Vector3 mins, ref Vector3 maxs )
		{
			_originalMins = mins;
			_originalMaxs = maxs;

			if ( IsActive )
				maxs = maxs.WithZ( 36 );
		}

		public float GetWishSpeed()
		{
			if ( !IsActive ) return -1;
			return 64.0f;
		}
	}
}
