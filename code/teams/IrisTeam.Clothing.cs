using System;
using Sandbox;
using Sandbox.Component;

namespace Facepunch.Hidden
{
	public partial class Player
	{
		bool OutfitChoice;
		Material skin;
		string beard;
		string femalefeatures;
		string facefeatures;

		public int outfit;

		public void Dress( Player ply )
		{
			OutfitChoice = !OutfitChoice;

			ply.SetMaterialGroup( "default" );
			if ( skin == null && beard == null && femalefeatures == null && facefeatures == null )
			{
				skin = Rand.FromArray( new[]
				{
				Material.Load ("models/citizen/skin/citizen_skin01.vmat"),
				Material.Load ("models/citizen/skin/citizen_skin03.vmat"),
				Material.Load ("models/citizen/skin/citizen_skin02.vmat"),
				Material.Load ("models/citizen/skin/citizen_skin04.vmat"),
				}
				);

				beard = Rand.FromArray( new[]
				{
				"models/citizen_clothes/hair/scruffy_beard/models/scruffy_beard_black.vmdl",
				"models/citizen_clothes/hair/scruffy_beard/models/scruffy_beard_brown.vmdl",
				"models/citizen_clothes/hair/scruffy_beard/models/scruffy_beard_grey.vmdl",
				"models/citizen_clothes/hair/stubble/model/stubble.vmdl",
				"models/citizen_clothes/hair/moustache/models/moustache_brown.vmdl",
				"models/citizen_clothes/hair/moustache/models/moustache_grey.vmdl",
				"",
				}
				);

				femalefeatures = Rand.FromArray( new[]
				{
				"models/citizen_clothes/hair/eyebrows_drawn/models/eyebrows_drawn.vmdl",
				"models/citizen_clothes/hair/eyelashes/models/eyelashes.vmdl",
				"",
				}
				);

				facefeatures = Rand.FromArray( new[]
{
				"models/citizen_clothes/makeup/face_tattoos/models/face_tattoos.vmdl",
				"models/citizen_clothes/makeup/freckles/model/freckles.vmdl",
				"",
				}
);
			}
			if ( outfit == 0 )
			{
				ply.SetMaterialOverride( skin, "skin" );
				ply.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/army_gloves.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest_army.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet_army.vmdl" );
				ply.AttachClothing( beard );
				ply.AttachClothing( facefeatures );
			}
			if (outfit == 1)
			{
				ply.SetMaterialOverride( skin, "skin" );
				ply.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/tactical_gloves.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/hat/balaclava/models/balaclava.vmdl" );
			}
			if (outfit == 2)
			{
				ply.SetMaterialOverride( skin, "skin" );
				ply.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/army_gloves.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest_army.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet_army.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/makeup/eyeliner/models/eyeliner.vmdl" );
				ply.AttachClothing( femalefeatures );
				ply.AttachClothing( facefeatures );
			}
			if ( outfit == 3 )
			{
				ply.SetMaterialOverride( skin, "skin" );
				ply.AttachClothing( "models/citizen_clothes/trousers/cargopants/models/cargo_pants.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/shirt/army_shirt/model/army_shirt.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/gloves/tactical_gloves/models/tactical_gloves.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/vest/tactical_vest/models/tactical_vest.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/shoes/boots/models/army_boots.vmdl" );
				ply.AttachClothing( "models/citizen_clothes/hat/tactical_helmet/models/tactical_helmet.vmdl" );
				ply.AttachClothing( femalefeatures );
				ply.AttachClothing( facefeatures );
			}

			//player.ClearMaterialOverride();
		}
	}
}
