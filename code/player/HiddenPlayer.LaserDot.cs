using Sandbox;

namespace Facepunch.Hidden
{
	public partial class HiddenPlayer
	{
		[Net] public LaserDot LaserDot { get; private set; }

		public void CreateLaserDot()
		{
			if ( !IsServer ) return;

			DestroyLaserDot();

			LaserDot = new LaserDot();
			LaserDot.Owner = this;

			PlaySound( "laser.on" );
		}

		public void DestroyLaserDot()
		{
			if ( LaserDot.IsValid() )
			{
				if ( IsServer )
				{
					LaserDot.Delete();
					LaserDot = null;
				}
				else
				{
					LaserDot.LaserParticles.Destroy( true );
					LaserDot.DotParticles.Destroy( true );
				}
			}
		}

		private void SimulateLaserDot( Client client )
		{
			if ( ActiveChild is Weapon weapon && weapon.HasLaserDot )
			{
				if ( LaserDot.IsValid() )
				{
					var position = IsServer ? EyePosition : Camera.Position;
					var rotation = IsServer ? EyeRotation : Camera.Rotation;

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
