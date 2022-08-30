
using Sandbox;
using Sandbox.UI;
using System.Linq;

namespace Facepunch.Hidden
{
	[UseTemplate]
	public partial class WeaponList : Panel
	{
		public static WeaponList Instance { get; private set; }

		public WeaponListItem[] Weapons { get; set; } = new WeaponListItem[6];

		private RealTimeUntil RemainOpenUntil { get; set; }

		[ClientRpc]
		public static void Expand( float duration )
        {
			if ( Instance != null )
            {
				Instance.RemainOpenUntil = duration;
			}
		}

		public WeaponList()
		{
			for ( int i = 0; i < 6; i++ )
			{
				var weapon = AddChild<WeaponListItem>( "weapon" );
				weapon.IsHidden = true;
				weapon.IsActive = false;
				Weapons[i] = weapon;
			}

			BindClass( "closed", IsCollapsed );

			Instance = this;
		}

		public bool IsCollapsed()
        {
			return RemainOpenUntil;
		}

		public override void Tick()
		{
			base.Tick();

			if ( Local.Pawn is not Player player )
				return;

			for ( int i = 0; i < Weapons.Length; i++ )
			{
				var weapon = Weapons[i];
				weapon.IsHidden = true;
			}

			var weapons = player.Children.OfType<Weapon>().ToList();
			weapons.Sort( ( a, b ) => a.Slot.CompareTo( b.Slot ) );

			var currentIndex = 0;

			foreach ( var child in weapons )
			{
				if ( currentIndex < Weapons.Length )
				{
					var weapon = Weapons[currentIndex];

					weapon.IsActive = (player.ActiveChild == child);
					weapon.IsHidden = false;
					weapon.IsAvailable = true;

					var keyBind = string.Empty;

					if ( weapon.IsAvailable )
                    {
						keyBind = IndexToSlotKey( currentIndex );
                    }

					weapon.KeyBind = keyBind;
					weapon.Update( child );

					currentIndex++;
				}
			}

			SetClass( "hidden", weapons.Count == 0 );
		}

		private string IndexToSlotKey( int index )
        {
			return $"iv_slot{index + 1}";
        }

		private int SlotPressInput( InputBuilder input )
		{
			if ( input.Pressed( InputButton.Slot1 ) ) return 1;
			if ( input.Pressed( InputButton.Slot2 ) ) return 2;
			if ( input.Pressed( InputButton.Slot3 ) ) return 3;
			if ( input.Pressed( InputButton.Slot4 ) ) return 4;
			if ( input.Pressed( InputButton.Slot5 ) ) return 5;
			if ( input.Pressed( InputButton.Slot6 ) ) return 6;

			return -1;
		}

		private bool CanSelectWeapon( WeaponListItem weapon )
		{
			if ( !weapon.IsHidden && weapon.Weapon.IsValid() )
			{
				return true;
			}

			return false;
		}

		private void PreviousWeapon( Player player, InputBuilder input )
		{
			var currentIndex = 0;

			for ( int i = 0; i < Weapons.Length; i++ )
			{
				var weapon = Weapons[i];

				if ( weapon.IsActive )
				{
					currentIndex = i;
					break;
				}
			}

			currentIndex--;

			for ( int i = currentIndex; i >= 0; i-- )
			{
				var weapon = Weapons[i];

				if ( !CanSelectWeapon( weapon ) )
					currentIndex--;
				else
					break;
			}

			var firstIndex = GetFirstIndex();
			var lastIndex = GetLastIndex();

			if ( currentIndex < firstIndex )
			{
				currentIndex = lastIndex;
			}

			SelectWeapon( player, input, currentIndex );
		}

		private void NextWeapon( Player player, InputBuilder input )
		{
			var currentIndex = 0;

			for ( int i = 0; i < Weapons.Length; i++ )
			{
				var weapon = Weapons[i];

				if ( weapon.IsActive )
				{
					currentIndex = i;
					break;
				}
			}

			currentIndex++;

			for ( int i = currentIndex; i < Weapons.Length; i++ )
			{
				var weapon = Weapons[i];

				if ( !CanSelectWeapon( weapon ) )
					currentIndex++;
				else
					break;
			}


			var firstIndex = GetFirstIndex();
			var lastIndex = GetLastIndex();

			if ( currentIndex > lastIndex )
				currentIndex = firstIndex;

			SelectWeapon( player, input, currentIndex );
		}

		private int GetLastIndex()
		{
			for ( int i = Weapons.Length - 1; i >= 0; i-- )
			{
				var weapon = Weapons[i];

				if ( CanSelectWeapon( weapon ) )
				{
					return i;
				}
			}

			return 0;
		}

		private int GetFirstIndex()
		{
			for ( int i = 0; i < Weapons.Length; i++ )
			{
				var weapon = Weapons[i];

				if ( CanSelectWeapon( weapon ) )
				{
					return i;
				}
			}

			return 0;
		}

		private void SelectWeapon( Player player, InputBuilder input, int index )
		{
			var weapon = Weapons[index];

			if ( CanSelectWeapon( weapon ) && player.ActiveChild != weapon.Weapon )
			{
				input.ActiveChild = weapon.Weapon;
				RemainOpenUntil = 3f;
			}
		}

		[Event.BuildInput]
		private void ProcessClientInput( InputBuilder input )
		{
			if ( Local.Pawn is not Player player )
				return;

			if ( input.MouseWheel == 1 )
			{
				NextWeapon( player, input );
				input.MouseWheel = 0;
			}
			else if ( input.MouseWheel == -1 )
			{
				PreviousWeapon( player, input );
				input.MouseWheel = 0;
			}
			else
			{
				var pressedInput = SlotPressInput( input );

				if ( pressedInput != -1 )
				{
					SelectWeapon( player, input, pressedInput - 1 );
				}
			}

			WeaponListItem activeWeapon = null;

			for ( int i = 0; i < Weapons.Length; i++ )
			{
				var weapon = Weapons[i];

				if ( weapon.IsActive )
				{
					activeWeapon = weapon;
					break;
				}
			}

			if ( activeWeapon != null && !CanSelectWeapon( activeWeapon ) )
			{
				var firstIndex = GetFirstIndex();
				var firstWeapon = Weapons[firstIndex];

				if ( CanSelectWeapon( firstWeapon ) )
					input.ActiveChild = firstWeapon.Weapon;
				else
					Input.ActiveChild = null;
			}
		}
	}
}
