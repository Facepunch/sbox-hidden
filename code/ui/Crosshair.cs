
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace HiddenGamemode
{
	public class Crosshair : Panel
	{
		public Label Health;

		private int _fireCounter;

		public Crosshair()
		{
			StyleSheet.Load( "/ui/Crosshair.scss" );

			for ( int i = 0; i < 5; i++ )
			{
				var p = Add.Panel( "element" );
				p.AddClass( $"el{i}" );
			}
		}

		public override void Tick()
		{
			base.Tick();
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
