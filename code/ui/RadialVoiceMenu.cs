using Sandbox;
using Sandbox.UI;
using System.Linq;

namespace Facepunch.Hidden
{
	[StyleSheet( "/ui/RadialVoiceMenu.scss")]
	public partial class RadialVoiceMenu : RadialMenu
	{
		public override InputButton Button => InputButton.View;

		public override void Populate()
		{
			var commands = ResourceLibrary.GetAll<RadioCommandResource>().ToList();

			foreach ( var command in commands )
			{
				AddRadioCommand( command.Text, command.ResourceId );
			}
		}

		protected override bool ShouldOpen()
		{
			return Local.Pawn is Player player && player.Team is IrisTeam;
		}

		private void AddRadioCommand( string name, int resourceId, string icon = null )
		{
			AddItem( name, "Play through radio", string.IsNullOrEmpty( icon ) ? "ui/icons/icon-ability-2.png" : icon, () => Player.PlayVoiceCmd( resourceId ) );
		}
	}
}
