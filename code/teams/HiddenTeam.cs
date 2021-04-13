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
		public Player CurrentPlayer { get; set; }

		private Abilities _abilitiesHud;

		public override void SupplyLoadout( Player player )
		{
			player.ClearAmmo();

			player.Inventory.DeleteContents();
			player.Inventory.Add( new Knife(), true );
		}

		public override void OnStart( Player player )
		{
			if ( Host.IsServer )
			{
				player.RemoveClothing();
			}

			player.Controller = new HiddenController();
			player.Camera = new FirstPersonCamera();
		}

		public override void OnTick()
		{
			if ( Host.IsClient )
			{
				if ( Sandbox.Player.Local is not Player localPlayer )
					return;

				if ( localPlayer.Team == this )
					return;

				var hidden = Game.Instance.GetTeamPlayers<HiddenTeam>( true ).FirstOrDefault();

				if ( hidden != null && hidden.IsValid() )
				{
					var distance = localPlayer.Pos.Distance( hidden.Pos );
					hidden.RenderAlpha = 0.2f - ((0.2f / 1500f) * distance);
				}
			}
			else
			{
				// TODO: When possible, here we're gonna make lights flicker when The Hidden is near them.

				/*
				var player = CurrentPlayer;

				if ( player != null && player.IsValid() )
				{
					var overlaps = Overlap.Sphere( player.Pos, 1024f );

					for ( int i = 0; i < overlaps.Count; ++i )
					{
						
					}
				}
				*/
			}
		}

		public override void OnTick( Player player )
		{
			if ( player.Input.Pressed( InputButton.Drop ) )
			{
				if ( player.Sense?.IsUsable() == true )
				{
					player.Sense.Use( player );
				}
			}

			if ( player.Input.Pressed( InputButton.View ) )
			{
				if ( player.Scream?.IsUsable() == true )
				{
					player.Scream.Use( player );
				}
			}
		}

		public override bool PlayPainSounds( Player player )
		{
			player.PlaySound( "hidden_grunt" + Rand.Int( 1, 2 ) );

			return true;
		}

		public override void OnJoin( Player player  )
		{
			Log.Info( $"{player.Name} joined the Hidden team." );

			if ( Host.IsClient && player.IsLocalPlayer )
			{
				_abilitiesHud = Sandbox.Hud.CurrentPanel.AddChild<Abilities>();
			}

			player.EnableShadowCasting = false;
			player.RenderAlpha = 0.15f;

			player.Sense = new SenseAbility();
			player.Scream = new ScreamAbility();

			CurrentPlayer = player;

			base.OnJoin( player );
		}

		public override void OnLeave( Player player )
		{
			player.EnableShadowCasting = true;
			player.RenderAlpha = 1f;

			Log.Info( $"{player.Name} left the Hidden team." );

			if ( _abilitiesHud != null && player.IsLocalPlayer )
			{
				_abilitiesHud.Delete( true );
				_abilitiesHud = null;
			}

			player.Sense = null;

			CurrentPlayer = null;

			base.OnLeave( player );
		}
	}
}
