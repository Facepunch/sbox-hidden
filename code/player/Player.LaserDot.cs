using Sandbox;

namespace HiddenGamemode
{
	partial class Player
	{
		private LaserDot _laserDot;

		private void CreateLaserDot()
		{
			DestroyLaserDot();
			_laserDot = new LaserDot();
		}

		private void DestroyLaserDot()
		{
			if ( _laserDot != null )
			{
				_laserDot.Delete();
				_laserDot = null;
			}
		}

		[Event.Frame]
		private void UpdateLaserDot()
		{
			if ( ActiveChild is Weapon weapon && weapon.HasLaserDot )
			{
				if ( _laserDot == null )
					CreateLaserDot();

				var trace = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 4096f )
					.UseHitboxes()
					.Radius( 2f )
					.Ignore( weapon )
					.Ignore( this )
					.Run();

				if ( trace.Hit )
					_laserDot.Position = trace.EndPosition;
			}
			else
			{
				DestroyLaserDot();
			}
		}
	}
}
