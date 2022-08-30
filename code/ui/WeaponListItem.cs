
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Linq;

namespace Facepunch.Hidden
{
	[UseTemplate]
	public class WeaponListItem : Panel
	{
		public bool IsAvailable { get; set; }
		public bool IsActive { get; set; }
		public bool IsHidden { get; set; }
		public string KeyBind { get; set; }
		public Panel SlotContainer { get; private set; }
		public Weapon Weapon { get; private set; }
		public Label Slot { get; private set; }
		public Image Icon { get; private set; }
		public Label Name { get; private set; }

		public void Update( Weapon weapon )
		{
			Weapon = weapon;
			Icon.Texture = Texture.Load( FileSystem.Mounted, weapon.Config.Icon );
			Name.Text = weapon.Config.Name;

			if ( !string.IsNullOrEmpty( KeyBind ) )
            {
				SlotContainer.SetClass( "hidden", false );
				Slot.Text = Input.GetKeyWithBinding( KeyBind ).ToUpper();
            }
			else
            {
				SlotContainer.SetClass( "hidden", true );
				Slot.Text = string.Empty;
            }

			var isHidden = (weapon.Owner is Player player && player.Team is HiddenTeam);

			SetClass( "team_red", isHidden );
			SetClass( "team_blue", !isHidden );
		}

		public override void Tick()
		{
			SetClass( "unavailable", !IsAvailable );
			SetClass( "hidden", IsHidden );
			SetClass( "active", IsActive );

			base.Tick();
		}
	}
}
