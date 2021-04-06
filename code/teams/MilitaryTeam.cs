using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace HiddenGamemode
{
    class MilitaryTeam : Team
	{
		public override void SupplyLoadout( Player player  )
		{
			player.ClearAmmo();

			player.Inventory.DeleteContents();

			player.Inventory.Add( new SMG(), true );
			player.Inventory.Add( new Shotgun(), true );

			player.GiveAmmo( AmmoType.Pistol, 100 );
			player.GiveAmmo( AmmoType.Buckshot, 8 );

			player.AttachClothing( "models/citizen_clothes/trousers/trousers.lab.vmdl" );
			player.AttachClothing( "models/citizen_clothes/jacket/labcoat.vmdl" );
			player.AttachClothing( "models/citizen_clothes/shoes/shoes.workboots.vmdl" );
			player.AttachClothing( "models/citizen_clothes/hat/hat_securityhelmet.vmdl" );
		}

		public override void OnJoin( Player player  )
		{
			Log.Info( $"{player.Name} joined the Military team." );

			player.Controller = new WalkController();
			player.Camera = new FirstPersonCamera();

			base.OnJoin( player );
		}

		public override void OnLeave( Player player  )
		{
			Log.Info( $"{player.Name} left the Military team." );

			base.OnLeave( player );
		}
	}
}
