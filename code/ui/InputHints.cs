using Sandbox.UI;
using Sandbox;

namespace Facepunch.Hidden
{
	public partial class InputHints : Panel
	{
		public static InputHints Current { get; private set; }

		public InputHint UseHint { get; private set; }
		public Panel Container { get; private set; }

		[ClientRpc]
		public static void UpdateOnClient()
		{
			Current?.Update();
		}

		public InputHints()
		{
			StyleSheet.Load( "/ui/InputHints.scss" );
			Container = Add.Panel( "container" );
			Current = this;

			Update();
		}

		public void Update()
		{
			Container.DeleteChildren( true );

			UseHint = null;

			if ( Local.Pawn is Player player )
			{
				if ( player.CameraMode is SpectateCamera )
				{
					AddHint( InputButton.PrimaryAttack, "Next" );
				}
				else if ( player.Team is HiddenTeam )
				{
					AddHint( InputButton.PrimaryAttack, "Slice" );
					AddHint( InputButton.SecondaryAttack, "Charge Stab" );
					UseHint = AddHint( InputButton.Use, "Grab, Throw, or Attach" );
					AddHint( InputButton.Jump, "Leap" );
				}
				else
				{
					AddHint( InputButton.PrimaryAttack, "Fire" );
					AddHint( InputButton.Reload, "Reload" );
					AddHint( InputButton.Jump, "Jump" );
				}
			}
		}

		private InputHint AddHint( InputButton button, string name )
		{
			var hint = Container.AddChild<InputHint>();
			hint.SetButton( button );
			hint.SetContent( name );
			return hint;
		}

		public override void Tick()
		{
			UpdateUseHint();
			base.Tick();
		}

		private void UpdateUseHint()
		{
			if ( UseHint == null ) return;
			if ( Local.Pawn is not Player player ) return;
			if ( player.Team is not HiddenTeam ) return;

			if ( player.Controller is HiddenController controller )
			{
				if ( !controller.GroundEntity.IsValid() )
				{
					if ( controller.IsFrozen )
						UseHint.SetContent( "Leap from Wall" );
					else
						UseHint.SetContent( "Attach to Wall" );
				}
				else if ( player.PickupEntity.IsValid() )
				{
					if ( player.PickupEntity is PlayerCorpse )
						UseHint.SetContent( "Attach Corpse to World" );
					else
						UseHint.SetContent( "Throw or Drop" );
				}
				else
				{
					UseHint.SetContent( "Pickup Object or Body" );
				}
			}
		}
	}
}
