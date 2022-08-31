using Sandbox;
using System;

namespace Facepunch.Hidden
{
    class HiddenTeam : BaseTeam
	{
		public override Color Color => Color.Parse( "#8a4a4a" ).Value;
		public override bool HideNameplate => true;
		public override string HudClassName => "team_hidden";
		public override string Name => "Hidden";
		public Player CurrentPlayer { get; set; }

		private float NextLightFlicker;
		private Abilities AbilitiesHud;

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

			player.RemoveClothing();

			player.SetModel( "models/citizen/citizen.vmdl" );

			player.SetBodyGroup( "Hands", 0 );
			player.SetBodyGroup( "Feet", 0 );
			player.SetBodyGroup( "Chest", 0 );
			player.SetBodyGroup( "Legs", 0 );

			player.EnableAllCollisions = true;
			player.EnableHideInFirstPerson = true;
			player.EnableShadowInFirstPerson = true;

			player.AttachClothing( "models/citizen_clothes/hair/hair_balding/models/hair_baldinggrey.vmdl" );
			player.AttachClothing( "models/citizen_clothes/jacket/hoodie/models/hoodie.vmdl" );
			player.AttachClothing( "models/citizen_clothes/shoes/sneakers/models/sneakers.vmdl" );
			player.AttachClothing( "models/citizen_clothes/trousers/trousers_tracksuit.vmdl" );

			player.Controller = new HiddenController();
			player.CameraMode = new HiddenFirstPersonCamera();

			player.DrawPlayer( false );
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
			if ( Host.IsServer )
			{
				if ( Time.Now <= NextLightFlicker )
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

				NextLightFlicker = Sandbox.Time.Now + Rand.Float( 2f, 5f );
			}
		}

		public override void Simulate( Player player )
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

			if ( Input.Pressed( InputButton.Use ) && !player.PickupEntity.IsValid() && player.TimeSinceDroppedEntity > 0.5f )
			{
				if ( player.Controller is not HiddenController controller )
					return;

				if ( controller.IsFrozen )
					return;

				var trace = Trace.Ray( player.EyePosition, player.EyePosition + player.EyeRotation.Forward * 40f )
					.WithAnyTags( "solid" )
					.Ignore( player )
					.Ignore( player.ActiveChild )
					.Radius( 2 )
					.Run();

				if ( trace.Hit )
				{
					controller.IsFrozen = true;
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
			if ( Host.IsClient && player.IsLocalPawn )
			{
				AbilitiesHud = Local.Hud.AddChild<Abilities>();
			}

			player.EnableShadowCasting = false;
			player.EnableShadowReceive = false;

			if ( Game.CanUseSense )
				player.Sense = new SenseAbility();
			else
				player.Sense = null;

			player.Scream = new ScreamAbility();

			CurrentPlayer = player;

			base.OnJoin( player );
		}

		public override void OnLeave( Player player )
		{
			player.EnableShadowReceive = true;
			player.EnableShadowCasting = true;

			Log.Info( $"{ player.Client.Name } left the Hidden team." );

			if ( AbilitiesHud != null && player.IsLocalPawn )
			{
				AbilitiesHud.Delete( true );
				AbilitiesHud = null;
			}

			player.Sense = null;

			CurrentPlayer = null;

			base.OnLeave( player );
		}
	}
}
