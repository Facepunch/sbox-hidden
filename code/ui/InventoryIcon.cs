
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace HiddenGamemode
{
	class InventoryIcon : Panel
	{
		public Weapon Weapon;
		public Panel Icon;

		public InventoryIcon( Weapon weapon )
		{
			Weapon = weapon;
			Icon = Add.Panel( "icon" );
			AddClass( weapon.ClassInfo.Name );
		}

		internal void TickSelection( Weapon selectedWeapon )
		{
			SetClass( "active", selectedWeapon == Weapon );
			SetClass( "empty", !Weapon?.IsUsable() ?? true );
		}

		public override void Tick()
		{
			base.Tick();

			if ( !Weapon.IsValid() || Weapon.Owner != Local.Pawn )
				Delete();
		}
	}
}

