
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace HiddenGamemode
{
	public class Battery : Panel
	{
		public Panel InnerBar;
		public Panel OuterBar;
		public Panel Icon;
		public Label Text;

		public Battery()
		{
			StyleSheet.Load( "/ui/Battery.scss" );

			Icon = Add.Panel( "icon" );
			OuterBar = Add.Panel( "outerBar" );
			InnerBar = OuterBar.Add.Panel( "innerBar" );
			Text = Add.Label( "0", "text" );
		}

		public override void Tick()
		{
			if ( Sandbox.Player.Local is not Player player ) return;

			SetClass( "hidden", player.LifeState != LifeState.Alive );
		}
	}
}
