﻿using Sandbox;

namespace Facepunch.Hidden
{
	public partial class ScreamAbility : BaseAbility
	{
		public override float Cooldown => 4;
		public override string Name => "Scream";

		private string[] ScreamSounds = new string[]
		{
			"scream-01",
			"scream-02",
			"scream-03",
			"scream-04"
		};

		public override string GetKeybind()
		{
			return Input.GetButtonOrigin( "view" ).ToUpper();
		}

		protected override void OnUse( HiddenPlayer player )
		{
			TimeSinceLastUse = 0;

			if ( Game.IsServer )
			{
				using ( Prediction.Off() )
				{
					PlayScreamSound( player );
				}
			}
		}

		private void PlayScreamSound( HiddenPlayer from )
		{
			var soundName = Game.Random.FromArray( ScreamSounds );
			from.PlaySound( soundName );
		}
	}
}

