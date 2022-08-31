using System;
using Sandbox;
using Sandbox.Component;

namespace Facepunch.Hidden
{
	class IrisTeam : BaseTeam
	{
		public override string HudClassName => "team_iris";
		public override Color Color => Color.Parse( "#4a8a59" ).Value;
		public override string Name => "I.R.I.S.";

		private Radar RadarHud;

		public override void SupplyLoadout( Player player )
		{
			player.ClearAmmo();
			player.Inventory.DeleteContents();
			player.Inventory.Add( new Pistol() { Slot = 2 }, true );

			if ( player.Deployment == DeploymentType.IRIS_ASSAULT )
			{
				player.Inventory.Add( new SMG() { Slot = 1 }, true );
				player.GiveAmmo( AmmoType.SMG, 90 );
			}
			else if ( player.Deployment == DeploymentType.IRIS_TACTICAL )
			{
				player.Inventory.Add( new Rifle() { Slot = 1 }, true );
				player.GiveAmmo( AmmoType.Rifle, 50 );
			}
			else
			{
				player.Inventory.Add( new Shotgun() { Slot = 1 }, true );
				player.GiveAmmo( AmmoType.Shotgun, 24 );
			}
		}
		public override void OnStart( Player player )
		{
			player.ClearAmmo();
			player.Inventory.DeleteContents();

			player.SetModel( "models/citizen/citizen.vmdl" );

			player.SetBodyGroup( "Hands", 1 );
			player.SetBodyGroup( "Feet", 1 );
			player.SetBodyGroup( "Chest", 1 );
			player.SetBodyGroup( "Legs", 1 );

			player.RemoveClothing();
			player.Dress(player);

			//player.ClearMaterialOverride();

			player.EnableAllCollisions = true;
			player.EnableDrawing = true;
			player.EnableHideInFirstPerson = true;
			player.EnableShadowInFirstPerson = true;

			player.Controller = new IrisController();
			player.CameraMode = new HiddenFirstPersonCamera();
		}

		public override void OnJoin( Player player )
		{
			if ( Host.IsClient && player.IsLocalPawn )
			{
				RadarHud = Local.Hud.AddChild<Radar>();	
			}
			base.OnJoin( player );
		}

		public override void AddDeployments( Deployment panel, Action<DeploymentType> callback )
		{
			panel.AddDeployment( new DeploymentInfo
			{
				Title = "ASSAULT",
				Description = "Sprints faster and is equipped with a high firerate SMG.",
				ClassName = "assault",
				OnDeploy = () => callback( DeploymentType.IRIS_ASSAULT )
			} );

			panel.AddDeployment( new DeploymentInfo
			{
				Title = "BRAWLER",
				Description = "Moves slower in general but is equipped with a high damage shotgun.",
				ClassName = "brawler",
				OnDeploy = () => callback( DeploymentType.IRIS_BRAWLER )
			} );

			panel.AddDeployment( new DeploymentInfo
			{
				Title = "TACTICAL",
				Description = "Sprints slower and is equipped with a burst mode rifle.",
				ClassName = "tactical",
				OnDeploy = () => callback( DeploymentType.IRIS_TACTICAL )
			} );
		}

		public override void OnPlayerKilled( Player player )
		{
			var playerglow = player.Components.GetOrCreate<Glow>();
			playerglow.Active = false;
			playerglow.Color = Color.White;
		}

		public override void OnLeave( Player player )
		{
			Log.Info( $"{player.Client.Name} left the Military team." );

			if ( player.IsLocalPawn )
			{
				if ( RadarHud != null )
				{
					RadarHud.Delete( true );
					RadarHud = null;
				}
			}

			base.OnLeave( player );
		}
	}
}
