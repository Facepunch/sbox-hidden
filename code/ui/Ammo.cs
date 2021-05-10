
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace HiddenGamemode
{
	public class Ammo : Panel
	{
		public Panel TextContainer;
		public Label Weapon;
		public Label Inventory;

		public Panel Icon;

		public Ammo()
		{
			
			TextContainer = Add.Panel( "textContainer" );
			Weapon = TextContainer.Add.Label( "100", "weapon" );
			Inventory = TextContainer.Add.Label( "100", "inventory" );
			Icon = Add.Panel( "icon" );
		}

		public override void Tick()
		{
			var player = Sandbox.Player.Local;
			if ( player == null ) return;

			var weapon = player.ActiveChild as Weapon;
			var isValid = (weapon != null && !weapon.IsMelee);

			SetClass( "active", isValid );
			SetClass("low-ammo", weapon != null && weapon.AmmoClip < 3);

			if ( !isValid ) return;

			Weapon.Text = $"{weapon.AmmoClip}";

			if ( !weapon.UnlimitedAmmo )
			{
				var inv = weapon.AvailableAmmo();
				Inventory.Text = $"/ {inv}";
				Inventory.SetClass( "active", inv >= 0 );
			}
			else
			{
				Inventory.Text = $"/ ∞";
				Inventory.SetClass( "active", true );
			}
		}
	}
}
