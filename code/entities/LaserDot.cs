using Sandbox;

namespace Facepunch.Hidden
{
	public partial class LaserDot : Entity
	{
		public Particles LaserParticles { get; private set; }
		public Particles DotParticles { get; private set; }

		public LaserDot()
		{
			Predictable = true;
			Transmit = TransmitType.Always;

			if ( IsClient )
			{
				DotParticles = Particles.Create( "particles/laserdot.vpcf" );
				DotParticles.SetEntity( 0, this, true );

				LaserParticles = Particles.Create( "particles/laserline.vpcf" );
			}
		}

		protected override void OnDestroy()
		{
			DotParticles?.Destroy( true );
			LaserParticles?.Destroy( true );

			base.OnDestroy();
		}
	}
}
