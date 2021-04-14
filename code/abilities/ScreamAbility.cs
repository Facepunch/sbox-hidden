using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public partial class ScreamAbility : BaseAbility
	{
		public override float Cooldown => 10;
		public override string Name => "Scream";

		private string[] _screamSounds = new string[]
		{
			"scream-01",
			"scream-02",
			"scream-03",
			"scream-04"
		};

		protected override void OnUse( Player player )
		{
			Log.Info( (Host.IsServer ? "Server: " : "Client: ") + "Time Since Last: " + TimeSinceLastUse );

			TimeSinceLastUse = 0;

			if ( Host.IsServer )
			{
				using ( Prediction.Off() )
				{
					PlayScreamSound( player );
				}
			}
		}

		private void PlayScreamSound( Player from )
		{
			var soundName = Rand.FromArray( _screamSounds );
			from.PlaySound( soundName );
		}
	}
}

