using Sandbox;
using Sandbox.UI;
using System.Linq;

namespace Facepunch.Hidden
{
	[UseTemplate]
	public partial class RadialVoiceMenu : SimpleRadialMenu
	{
		public override InputButton Button => InputButton.View;

		public override void Populate()
		{
			var commands = ResourceLibrary.GetAll<RadioCommandResource>().ToList();

			foreach ( var command in commands )
			{
				AddRadioCommand( command.Text, command.ResourceId );
			}

			base.Populate();
		}

		protected override bool ShouldOpen()
		{
			return Local.Pawn is Player player && player.Team is IrisTeam;
		}

		private void AddRadioCommand( string name, int resourceId, string icon = null )
		{
			AddAction( name, "Play through radio", string.IsNullOrEmpty( icon ) ? "ui/icons/icon-ability-2.png" : icon, () => Player.PlayVoiceCmd( resourceId ) );
		}
	}
}
