using Sandbox;

namespace Facepunch.Hidden
{
	public partial class ScreamAbility : BaseAbility
	{
		public override float Cooldown => 10;
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
			return Input.GetKeyWithBinding( "iv_view" ).ToUpper();
		}

		protected override void OnUse( Player player )
		{
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
			var soundName = Rand.FromArray( ScreamSounds );
			from.PlaySound( soundName );
		}
	}
}

