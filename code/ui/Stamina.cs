
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace HiddenGamemode
{
	public class Stamina : Panel
	{
		public Panel InnerBar;
		public Panel OuterBar;
		public Panel Icon;

		public Stamina()
		{
			StyleSheet.Load( "/ui/Stamina.scss" );

			Icon = Add.Panel( "icon" );
			OuterBar = Add.Panel( "outerBar" );
			InnerBar = OuterBar.Add.Panel( "innerBar" );
		}

		public override void Tick()
		{
			if ( Sandbox.Player.Local is not Player player )
				return;

			SetClass( "hidden", player.LifeState != LifeState.Alive );

			InnerBar.Style.Width = Length.Percent( player.Stamina );
			InnerBar.Style.Dirty();
		}
	}
}
