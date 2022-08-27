using Sandbox;

namespace Facepunch.Hidden
{
	public partial class LaserDot : Entity
	{
		private Particles Particles;

		public LaserDot()
		{
			Predictable = true;
			Transmit = TransmitType.Always;

			if ( IsClient )
			{
				Particles = Particles.Create( "particles/laserdot.vpcf" );

				if ( Particles != null )
					Particles.SetEntity( 0, this, true );
			}
		}

		protected override void OnDestroy()
		{
			Particles?.Destroy( true );
			base.OnDestroy();
		}
	}
}
