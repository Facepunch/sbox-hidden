
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	[ClassLibrary]
	public partial class Hud : Sandbox.Hud
	{
		public Hud()
		{
			if ( !IsClient )
				return;

			RootPanel.StyleSheet.Load( "/ui/Hud.scss" );

			RootPanel.AddChild<RoundInfo>();
			RootPanel.AddChild<Vitals>();
			RootPanel.AddChild<Stamina>();
			RootPanel.AddChild<Ammo>();
			RootPanel.AddChild<Nameplates>();
			RootPanel.AddChild<DamageIndicator>();
			RootPanel.AddChild<HitIndicator>();
			RootPanel.AddChild<InventoryBar>();
			RootPanel.AddChild<ChatBox>();
			RootPanel.AddChild<Scoreboard>();
		}
	}
}
