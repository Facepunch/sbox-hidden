using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
    class HiddenTeam : BaseTeam
	{
		public override void SupplyLoadout( Player player )
		{
			player.ClearAmmo();

			player.Inventory.DeleteContents();
			player.Inventory.Add( new Pistol(), true );

			player.GiveAmmo( AmmoType.Pistol, 100 );
			player.GiveAmmo( AmmoType.Buckshot, 8 );

			player.AttachClothing( "models/citizen_clothes/dress/dress.kneelength.vmdl" );
			player.AttachClothing( "models/citizen_clothes/hair/hair_femalebun.black.vmdl" );
		}

		public override void OnJoin( Player player  )
		{
			Log.Info( $"{player.Name} joined the Hidden team." );

			// TODO: Tweak these values to perfection.
			var controller = new HiddenController
			{
				AirAcceleration = 20f,
				SprintSpeed = 380f,
				AirControl = 10f,
				Gravity = 400f
			};

			player.Controller = controller;

			player.Camera = new FirstPersonCamera();

			base.OnJoin( player );
		}

		public override void OnLeave( Player player )
		{
			Log.Info( $"{player.Name} left the Hidden team." );

			base.OnLeave( player );
		}
	}
}
