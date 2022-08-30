
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Facepunch.Hidden
{
	public class Health : Panel
	{
		public Panel InnerBar;
		public Panel OuterBar;
		public Label Text;

		public Health()
		{
			StyleSheet.Load( "/ui/Health.scss" );

			OuterBar = Add.Panel( "outerBar" );
			InnerBar = OuterBar.Add.Panel( "innerBar" );
			Text = Add.Label( "0", "text" );
		}

		public override void Tick()
		{
			if ( Local.Pawn is not Player player ) return;

			SetClass( "hidden", player.LifeState != LifeState.Alive );
			SetClass("low-health", player.Health < 30);

			InnerBar.Style.Width = Length.Percent( player.Health );

			Text.Text = ((int)player.Health).ToString();
		}
	}
}
