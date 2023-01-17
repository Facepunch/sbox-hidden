using System;
using System.Linq;
using Sandbox;
using Sandbox.Component;

namespace Facepunch.Hidden
{
	class IrisTeam : BaseTeam
	{
		public override string HudClassName => "team_iris";
		public override Color Color => Color.Parse( "#4a8a59" ).Value;
		public override string Name => "I.R.I.S.";

		public virtual void DressPlayer( HiddenPlayer player )
		{
			Game.SetRandomSeed( player.UniqueRandomSeed );

			player.SetMaterialGroup( Game.Random.Int( 3 ) );

			var beard = Game.Random.FromArray( new[]
			{
				"models/citizen_clothes/hair/scruffy_beard/models/scruffy_beard_black.vmdl",
				"models/citizen_clothes/hair/scruffy_beard/models/scruffy_beard_brown.vmdl",
				"models/citizen_clothes/hair/scruffy_beard/models/scruffy_beard_grey.vmdl",
				"models/citizen_clothes/hair/stubble/model/stubble.vmdl",
				"models/citizen_clothes/hair/moustache/models/moustache_brown.vmdl",
				"models/citizen_clothes/hair/moustache/models/moustache_grey.vmdl",
				string.Empty
			} );

			var femaleFeatures = Game.Random.FromArray( new[]
			{
				"models/citizen_clothes/hair/eyebrows_drawn/models/eyebrows_drawn.vmdl",
				"models/citizen_clothes/hair/eyelashes/models/eyelashes.vmdl",
				string.Empty,
			} );

			var faceFeatures = Game.Random.FromArray( new[]
			{
				"models/citizen_clothes/makeup/face_tattoos/models/face_tattoos.vmdl",
				"models/citizen_clothes/makeup/freckles/model/freckles.vmdl",
				string.Empty
			} );

			var outfit = Game.Random.Int( 3 );

			if ( outfit == 0 )
			{
				player.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
				player.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
				player.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/army_gloves.vmdl" );
				player.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest_army.vmdl" );
				player.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
				player.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet_army.vmdl" );
				player.AttachClothing( beard );
				player.AttachClothing( faceFeatures );
			}
			else if ( outfit == 1 )
			{
				player.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
				player.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
				player.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/tactical_gloves.vmdl" );
				player.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest.vmdl" );
				player.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
				player.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet.vmdl" );
				player.AttachClothing( "models/citizen_clothes/hat/balaclava/models/balaclava.vmdl" );
			}
			else if ( outfit == 2 )
			{
				player.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
				player.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
				player.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/army_gloves.vmdl" );
				player.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest_army.vmdl" );
				player.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
				player.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet_army.vmdl" );
				player.AttachClothing( "models/citizen_clothes/makeup/eyeliner/models/eyeliner.vmdl" );
				player.AttachClothing( femaleFeatures );
				player.AttachClothing( faceFeatures );
			}
			else if ( outfit == 3 )
			{
				player.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
				player.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
				player.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/tactical_gloves.vmdl" );
				player.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest.vmdl" );
				player.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
				player.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet.vmdl" );
				player.AttachClothing( femaleFeatures );
				player.AttachClothing( faceFeatures );
			}
		}

		public override void SupplyLoadout( HiddenPlayer player )
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

		public override void OnStart( HiddenPlayer player )
		{
			player.ClearAmmo();
			player.Inventory.DeleteContents();

			player.SetModel( "models/citizen/citizen.vmdl" );

			player.SetBodyGroup( "Hands", 1 );
			player.SetBodyGroup( "Feet", 1 );
			player.SetBodyGroup( "Chest", 1 );
			player.SetBodyGroup( "Legs", 1 );

			player.RemoveClothing();

			DressPlayer( player );

			//player.ClearMaterialOverride();

			player.EnableAllCollisions = true;
			player.EnableDrawing = true;
			player.EnableHideInFirstPerson = true;
			player.EnableShadowInFirstPerson = true;

			player.SetMoveController<IrisController>();
		}

		public override void AddDeployments( Deployment panel, Action<DeploymentType> callback )
		{
			panel.AddDeployment( new Deployment.DeploymentInfo
			{
				Title = "ASSAULT",
				Description = "Sprints faster and is equipped with a high firerate SMG.",
				ClassName = "assault",
				OnDeploy = () => callback( DeploymentType.IRIS_ASSAULT )
			} );

			panel.AddDeployment( new Deployment.DeploymentInfo
			{
				Title = "BRAWLER",
				Description = "Moves slower in general but is equipped with a high damage shotgun.",
				ClassName = "brawler",
				OnDeploy = () => callback( DeploymentType.IRIS_BRAWLER )
			} );

			panel.AddDeployment( new Deployment.DeploymentInfo
			{
				Title = "TACTICAL",
				Description = "Sprints slower and is equipped with a burst mode rifle.",
				ClassName = "tactical",
				OnDeploy = () => callback( DeploymentType.IRIS_TACTICAL )
			} );
		}
	}
}
