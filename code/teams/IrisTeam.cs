using System;
using Sandbox;
using Sandbox.Component;

namespace Facepunch.Hidden
{
    class IrisTeam : BaseTeam
	{
		public override string HudClassName => "team_iris";
		public override string Name => "I.R.I.S.";

		private Radar RadarHud;

		public override void SupplyLoadout( Player player  )
		{
			player.ClearAmmo();
			player.Inventory.DeleteContents();
			player.Inventory.Add( new Pistol(), true );

			if ( player.Deployment == DeploymentType.IRIS_ASSAULT )
			{
				player.Inventory.Add( new SMG(), true );
				player.GiveAmmo( AmmoType.Pistol, 120 );
			}
			else
			{
				player.Inventory.Add( new Shotgun(), true );
				player.GiveAmmo( AmmoType.Shotgun, 16 );
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
			player.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
			player.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
			player.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/army_gloves.vmdl" );
			player.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest_army.vmdl" );
			player.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
			player.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet_army.vmdl" );

			player.ClearMaterialOverride();

			player.EnableAllCollisions = true;
			player.EnableDrawing = true;
			player.EnableHideInFirstPerson = true;
			player.EnableShadowInFirstPerson = true;

			player.Controller = new IrisController();
			player.CameraMode = new HiddenFirstPersonCamera();
		}

		public override void OnJoin( Player player  )
		{
			Log.Info( $"{ player.Client.Name } joined the Military team." );

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
				Description = "Moves slower and is equipped with a high damage shotgun.",
				ClassName = "brawler",
				OnDeploy = () => callback( DeploymentType.IRIS_BRAWLER )
			} );
		}

		public override void OnPlayerKilled( Player player )
		{
			var playerglow = player.Components.GetOrCreate<Glow>();
			playerglow.Active = false;
			playerglow.Color = Color.White;
		}

		public override void OnLeave( Player player  )
		{
			Log.Info( $"{ player.Client.Name } left the Military team." );

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
