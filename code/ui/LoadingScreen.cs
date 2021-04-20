
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
			var isHidden = false;

			if ( Sandbox.Player.Local is Player player )
			{
				if ( player.Team != null )
					isHidden = true;

				if ( player.IsSpectator && !player.HasSpectatorTarget )
				{
					if ( player.SpectatorDeathPosition.IsNearlyZero() )
						isHidden = false;
				}
			}

			SetClass( "hidden", isHidden );

			base.Tick();
		}
	}
}
