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
					var position = IsServer ? EyePosition : Input.Position;
					var rotation = IsServer ? EyeRotation : Input.Rotation;

					if ( IsServer )
					{
						var attachment = weapon.GetAttachment( "laser" );

						if ( attachment.HasValue )
						{
							position = attachment.Value.Position;
							rotation = attachment.Value.Rotation;
						}
					}

					var trace = Trace.Ray( position, position + rotation.Forward * 4096f )
						.UseHitboxes()
						.Radius( 2f )
						.Ignore( weapon )
						.Ignore( this )
						.Run();

					LaserDot.Position = trace.EndPosition;
				}
			}
			else if ( IsServer )
			{
				DestroyLaserDot();
			}
		}
	}
}
