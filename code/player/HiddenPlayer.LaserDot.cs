using Sandbox;

namespace Facepunch.Hidden
{
	public partial class HiddenPlayer
	{
		[Net] public LaserDot LaserDot { get; private set; }

		public void CreateLaserDot()
		{
			if ( !Game.IsServer ) return;

			DestroyLaserDot();

			LaserDot = new LaserDot();
			LaserDot.Owner = this;

			PlaySound( "laser.on" );
		}

		public void DestroyLaserDot()
		{
			if ( LaserDot.IsValid() )
			{
				if ( Game.IsServer )
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

		private void SimulateLaserDot( IClient client )
		{
			if ( ActiveChild is Weapon weapon && weapon.HasLaserDot )
			{
				if ( LaserDot.IsValid() )
				{
					var position = Game.IsServer ? EyePosition : Camera.Position;
					var rotation = Game.IsServer ? EyeRotation : Camera.Rotation;

					if ( Game.IsServer )
					{
						var attachment = weapon.GetAttachment( "laser" ) ?? weapon.GetAttachment( "muzzle" );

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
			else if ( Game.IsServer )
			{
				DestroyLaserDot();
			}
		}
	}
}
