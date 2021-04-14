
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace HiddenGamemode
{
	public class LoadingScreen : Panel
	{
		public Label Text;

		public LoadingScreen()
		{
			StyleSheet.Load( "/ui/LoadingScreen.scss" );

			Text = Add.Label( "Loading", "loading" );
		}

		public override void Tick()
		{
			var doesLocalPlayerHaveTeam = false;

			if ( Sandbox.Player.Local is Player player )
			{
				if ( player.Team != null )
					doesLocalPlayerHaveTeam = true;
			}

			SetClass( "hidden", doesLocalPlayerHaveTeam );

			base.Tick();
		}
	}
}
