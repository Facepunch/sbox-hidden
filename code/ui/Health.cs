
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace HiddenGamemode
{
	public class Health : Panel
	{
		public Panel InnerBar;
		public Panel OuterBar;
		public Panel Icon;
		public Label Text;

		public Health()
		{
			StyleSheet.Load( "/ui/Health.scss" );

			Icon = Add.Panel( "icon" );
			OuterBar = Add.Panel( "outerBar" );
			InnerBar = OuterBar.Add.Panel( "innerBar" );
			Text = Add.Label( "0", "text" );
		}

		public override void Tick()
		{
			if ( Sandbox.Player.Local is not Player player ) return;

			SetClass( "hidden", player.LifeState != LifeState.Alive );
			SetClass("low-health", player.Health < 30);

			InnerBar.Style.Width = Length.Percent( player.Health );
			InnerBar.Style.Dirty();
			

			Text.Text = player.Health.ToString();
		}
	}
}
