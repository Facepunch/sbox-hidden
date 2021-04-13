using Sandbox;

namespace HiddenGamemode
{
	[ClassLibrary]
	public class DuckController : NetworkClass
	{
		public BasePlayerController Controller { get; set; }
		public bool IsActive { get; set; }

		public DuckController( BasePlayerController controller )
		{
			Controller = controller;
		}

		public virtual void PreTick()
		{
			bool isHoldingDuck = Controller.Input.Down( InputButton.Duck );

			if ( isHoldingDuck != IsActive )
			{
				if ( isHoldingDuck )
					TryDuck();
				else
					TryUnDuck();
			}

			if ( IsActive )
			{
				Controller.SetTag( "ducked" );
				Controller.ViewOffset *= 0.5f;
			}
		}

		private void TryDuck()
		{
			IsActive = true;
		}

		private void TryUnDuck()
		{
			var pm = Controller.TraceBBox( Controller.Pos, Controller.Pos, _originalMins, _originalMaxs );

			if ( pm.StartedSolid ) return;

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
