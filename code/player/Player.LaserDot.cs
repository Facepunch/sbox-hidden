using Sandbox;

namespace Facepunch.Hidden
{
	public partial class Player
	{
		[Net] private LaserDot LaserDot { get; set; }

		private void CreateLaserDot()
		{
			DestroyLaserDot();

			LaserDot = new LaserDot();
			LaserDot.Owner = this;
		}

		private void DestroyLaserDot()
		{
			if ( LaserDot != null )
			{
				LaserDot.Delete();
				LaserDot = null;
			}
		}

		private void SimulateLaserDot( Client client )
		{
			if ( ActiveChild is Weapon weapon && weapon.HasLaserDot )
			{
				if ( IsServer && LaserDot == null )
				{
					CreateLaserDot();
				}

				if ( LaserDot.IsValid() )
				{
					var trace = Trace.Ray( Input.Position, Input.Position + Input.Rotation.Forward * 4096f )
						.UseHitboxes()
						.Radius( 2f )
						.Ignore( weapon )
						.Ignore( this )
						.Run();

					if ( trace.Hit )
					{
						LaserDot.Position = trace.EndPosition;
					}
				}
			}
			else if ( IsServer )
			{
				DestroyLaserDot();
			}
		}
	}
}
