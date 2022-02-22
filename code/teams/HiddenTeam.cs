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
		public override string Name => "Hidden";
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
			player.CameraMode = new FirstPersonCamera();
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
				if ( Local.Pawn is not Player localPlayer )
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
				if ( Time.Now <= _nextLightFlicker )
					return;

				var player = CurrentPlayer;

				if ( player != null && player.IsValid() )
				{
					var overlaps = Entity.FindInSphere( player.Position, 2048f );

					foreach ( var entity in overlaps )
					{
						// Make sure we don't also flicker flashlights.
						if ( entity is SpotLightEntity light && entity is not Flashlight )
						{
							if ( Rand.Float( 0f, 1f ) >= 0.5f )
								Game.Instance.LightFlickers.Add( light, Rand.Float( 0.5f, 2f ) );
						}
					}
				}

				_nextLightFlicker = Sandbox.Time.Now + Rand.Float( 2f, 5f );
			}
		}

		public override void OnTick( Player player )
		{
			if ( Input.Pressed( InputButton.Drop ) )
			{
				if ( player.Sense?.IsUsable( player ) == true )
				{
					player.Sense.Use( player );
				}
			}

			if ( Input.Pressed( InputButton.View ) )
			{
				if ( player.Scream?.IsUsable( player ) == true )
				{
					player.Scream.Use( player );
				}
			}

			if ( Input.Pressed( InputButton.Use) )
			{
				if ( player.Controller is not HiddenController controller )
					return;

				if ( controller.IsFrozen )
					return;

				var trace = Trace.Ray( player.EyePosition, player.EyePosition + player.EyeRotation.Forward * 40f )
					.HitLayer( CollisionLayer.WORLD_GEOMETRY )
					.Ignore( player )
					.Ignore( player.ActiveChild )
					.Radius( 2 )
					.Run();

				if ( trace.Hit )
					controller.IsFrozen = true;
			}
		}

		public override bool PlayPainSounds( Player player )
		{
			player.PlaySound( "hidden_grunt" + Rand.Int( 1, 2 ) );

			return true;
		}

		public override void OnJoin( Player player  )
		{
			Log.Info( $"{ player.Client.Name } joined the Hidden team." );

			if ( Host.IsClient && player.IsLocalPawn )
			{
				_abilitiesHud = Local.Hud.AddChild<Abilities>();
			}

			player.EnableShadowCasting = false;
			player.EnableShadowReceive = false;
			player.RenderColor = player.RenderColor.WithAlpha(0.15f);

			player.Sense = new SenseAbility();
			player.Scream = new ScreamAbility();

			CurrentPlayer = player;

			base.OnJoin( player );
		}

		public override void OnLeave( Player player )
		{
			player.EnableShadowReceive = true;
			player.EnableShadowCasting = true;
			player.RenderColor = player.RenderColor.WithAlpha(1f);

			Log.Info( $"{ player.Client.Name } left the Hidden team." );

			if ( _abilitiesHud != null && player.IsLocalPawn )
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
