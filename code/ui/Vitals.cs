
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace HiddenGamemode
{
	public class Vitals : Panel
	{
		public Label Health;

		public Vitals()
		{
			Health = Add.Label( "100", "health" );
		}

		public override void Tick()
		{
			var player = Sandbox.Player.Local as Player;
			if ( player == null ) return;

			SetClass( "hidden", player.IsSpectator );

			Health.Text = $"{player.Health:n0}";
			Health.SetClass( "danger", player.Health < 40.0f );
		}
	}
}
