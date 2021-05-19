
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace HiddenGamemode
{
	public class Crosshair : Panel
	{
		public Panel ChargeBackgroundBar;
		public Panel ChargeForegroundBar;
		public Panel Charge;

		private int _fireCounter;

		public Crosshair()
		{
			StyleSheet.Load( "/ui/Crosshair.scss" );

			for ( int i = 0; i < 5; i++ )
			{
				var p = Add.Panel( "element" );
				p.AddClass( $"el{i}" );
			}

			Charge = Add.Panel( "charge" );
			ChargeBackgroundBar = Charge.Add.Panel( "background" );
			ChargeForegroundBar = ChargeBackgroundBar.Add.Panel( "foreground" );
		}

		public override void Tick()
		{
			base.Tick();

			if ( Local.Pawn is not Player player )
				return;

			Charge.SetClass( "hidden", true );

			if ( player.ActiveChild is Weapon weapon )
			{
				if ( weapon.ChargeAttackEndTime > 0f && Time.Now < weapon.ChargeAttackEndTime )
				{
					var timeLeft = weapon.ChargeAttackEndTime - Time.Now;

					ChargeForegroundBar.Style.Width = Length.Percent( 100f - ((100f / weapon.ChargeAttackDuration) * timeLeft) );
					ChargeForegroundBar.Style.Dirty();

					Charge.SetClass( "hidden", false );
				}
			}

			this.PositionAtCrosshair();

			SetClass( "fire", _fireCounter > 0 );

			if ( _fireCounter > 0 )
				_fireCounter--;
		}

		public override void OnEvent( string eventName )
		{
			if ( eventName == "fire" )
			{
				_fireCounter += 2;
			}

			base.OnEvent( eventName );
		}
	}
}
