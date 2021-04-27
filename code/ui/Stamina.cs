
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace HiddenGamemode
{
	public class Stamina : Panel
	{
		public Panel InnerBar;
		public Panel OuterBar;
		public Panel Icon;
		public Label Text;

		public Stamina()
		{
			StyleSheet.Load( "/ui/Stamina.scss" );

			Icon = Add.Panel( "icon" );
			OuterBar = Add.Panel( "outerBar" );
			InnerBar = OuterBar.Add.Panel( "innerBar" );
			Text = Add.Label( "0", "text" );
		}

		public override void Tick()
		{
			if ( Sandbox.Player.Local is not Player player ) return;

			SetClass( "hidden", player.LifeState != LifeState.Alive );
			SetClass("low-stamina", player.Stamina < 30);

			InnerBar.Style.Width = Length.Percent( player.Stamina );
			InnerBar.Style.Dirty();

			Text.Text = ((int)player.Stamina).ToString();
		}
	}
}
