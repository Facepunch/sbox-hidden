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
		public override bool HideNameplate => true;
		public override string HudClassName => "team_hidden";

		private Abilities AbilitiesHud;

		public override void SupplyLoadout( Player player )
		{
			player.ClearAmmo();

			player.Inventory.DeleteContents();
			player.Inventory.Add( new Knife(), true );
		}

		public override void OnTick( Player player )
		{
			if ( player.Input.Pressed( InputButton.Drop ) )
			{
				if ( player.Sense?.IsUsable() == true )
				{
					player.Sense.OnUse( player );
				}
			}

			if ( player.Input.Pressed( InputButton.View ) )
			{
				if ( player.Scream?.IsUsable() == true )
				{
					player.Scream.OnUse( player );
				}
			}
		}

		public override void OnJoin( Player player  )
		{
			Log.Info( $"{player.Name} joined the Hidden team." );

			if ( Host.IsServer )
			{
				player.RemoveClothing();
			}

			if ( Host.IsClient && player.IsLocalPlayer )
			{
				AbilitiesHud = Sandbox.Hud.CurrentPanel.AddChild<Abilities>();
			}

			// TODO: Tweak these values to perfection.
			var controller = new HiddenController
			{
				AirAcceleration = 20f,
				SprintSpeed = 380f,
				AirControl = 10f,
				Gravity = 400f
			};

			player.EnableShadowCasting = false;
			player.RenderAlpha = 0.12f;
			player.Controller = controller;
			player.Camera = new FirstPersonCamera();

			player.Sense = new SenseAbility();
			player.Scream = new ScreamAbility();

			base.OnJoin( player );
		}

		public override void OnLeave( Player player )
		{
			player.EnableShadowCasting = true;
			player.RenderAlpha = 1f;

			Log.Info( $"{player.Name} left the Hidden team." );

			if ( AbilitiesHud != null )
			{
				Log.Info( "Deleting Ability HUD" );

				AbilitiesHud.Delete( true );
			}

			player.Sense = null;

			base.OnLeave( player );
		}
	}
}
