
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace HiddenGamemode
{
	public class Vitals : Panel
	{
		public Stamina Stamina;
		public Health Health;

		public Vitals()
		{
			Health = AddChild<Health>();
			Stamina = AddChild<Stamina>();
		}

		public override void Tick()
		{
			if ( Sandbox.Player.Local is not Player player ) return;

			SetClass( "hidden", player.IsSpectator );

			Health.Tick();
			Stamina.Tick();
		}
	}
}
