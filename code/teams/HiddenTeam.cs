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

		private float _nextLightFlicker;
		private Abilities _abilitiesHud;

		public override void SupplyLoadout( Player player )
		{
			player.ClearAmmo();
			player.Inventory.DeleteContents();
			player.Inventory.Add( new Knife(), true );
		}

		public override void OnStart( Player player )
		{
			player.ClearAmmo();
			player.Inventory.DeleteContents();

			if ( Host.IsServer )
			{
				player.RemoveClothing();
			}

			player.SetModel( "models/citizen/citizen.vmdl" );

			player.EnableAllCollisions = true;
			player.EnableDrawing = true;
			player.EnableHideInFirstPerson = true;
			player.EnableShadowInFirstPerson = true;

			player.Controller = new HiddenController();
			player.Camera = new FirstPersonCamera();
		}

		public override void AddDeployments( Deployment panel, Action<DeploymentType> callback )
		{
			panel.AddDeployment( new DeploymentInfo
			{
				Title = "CLASSIC",
				Description = "Well rounded and recommended for beginners.",
				ClassName = "classic",
				OnDeploy = () => callback( DeploymentType.HIDDEN_CLASSIC )
			} );

			panel.AddDeployment( new DeploymentInfo
			{
				Title = "BEAST",
				Description = "Harder to kill but moves slower. Deals more damage. Sense ability can be used more frequently.",
				ClassName = "beast",
				OnDeploy = () => callback( DeploymentType.HIDDEN_BEAST )
			} );

			panel.AddDeployment( new DeploymentInfo
			{
				Title = "ROGUE",
				Description = "Moves faster but easier to kill. Deals less damage. Sense ability can be used less frequently.",
				ClassName = "rogue",
				OnDeploy = () => callback( DeploymentType.HIDDEN_ROGUE )
			} );
		}

		public override void OnTakeDamageFromPlayer( Player player, Player attacker, DamageInfo info )
		{
			if ( player.Deployment == DeploymentType.HIDDEN_BEAST )
			{
				info.Damage *= 0.5f;
			}
			else if ( player.Deployment == DeploymentType.HIDDEN_ROGUE )
			{
				info.Damage *= 1.5f;
			}
		}

		public override void OnDealDamageToPlayer( Player player, Player target, DamageInfo info )
		{
			if ( player.Deployment == DeploymentType.HIDDEN_BEAST )
			{
				info.Damage *= 1.25f;
			}
			else if ( player.Deployment == DeploymentType.HIDDEN_ROGUE )
			{
				info.Damage *= 0.75f;
			}
		}

		public override void OnTick()
		{
			if ( Host.IsClient )
			{
				/*
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
				*/
			}
			else
			{
				if ( Sandbox.Time.Now > _nextLightFlicker )
				{
					var player = CurrentPlayer;

					if ( player != null && player.IsValid() )
					{
						var overlaps = Overlap.Sphere( player.Pos, 2048f );
						var eligible = new List<SpotLightEntity>();

						for ( int i = 0; i < overlaps.Count; ++i )
						{
							if ( overlaps.Element( i ).Entity is SpotLightEntity light )
							{
								eligible.Add( light );
							}
						}

						if ( eligible.Count > 0 )
						{
							var random = eligible[Rand.Int( 0, eligible.Count - 1 )];
							_ = FlickerLight( random );
						}
					}

					_nextLightFlicker = Sandbox.Time.Now + Rand.Float( 5f, 20f );
				}
			}
		}

		private async Task FlickerLight( SpotLightEntity light )
		{
			var wasFlickering = light.Flicker;
			var oldBrightness = light.Brightness;

			light.Flicker = true;
			light.Brightness = Rand.Float( 0f, 1f );

			await Task.Delay( Rand.Int( 50, 500 ) );

			light.Brightness = oldBrightness;
			light.Flicker = wasFlickering;
		}

		public override void OnTick( Player player )
		{
			if ( player.Input.Pressed( InputButton.Drop ) )
			{
				if ( player.Sense?.IsUsable( player ) == true )
				{
					player.Sense.Use( player );
				}
			}

			if ( player.Input.Pressed( InputButton.View ) )
			{
				if ( player.Scream?.IsUsable( player ) == true )
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
			player.EnableShadowReceive = false;
			player.RenderAlpha = 0.15f;

			player.Sense = new SenseAbility();
			player.Scream = new ScreamAbility();

			CurrentPlayer = player;

			base.OnJoin( player );
		}

		public override void OnLeave( Player player )
		{
			player.EnableShadowReceive = true;
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
