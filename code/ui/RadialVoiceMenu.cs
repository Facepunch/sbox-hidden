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
			AddRadioCommand( "Subject spotted!", "subject_spotted" );
			AddRadioCommand( "He's on me!", "on_me" );
			AddRadioCommand( "Help!", "help" );
			AddRadioCommand( "Where are you...", "where_are_you" );
			AddRadioCommand( "Man down!", "man_down" );

			base.Populate();
		}

		protected override bool ShouldOpen( InputBuilder builder )
		{
			return Local.Pawn is Player player && player.Team is IrisTeam;
		}

		private void AddRadioCommand( string name, string command, string icon = null )
		{
			AddAction( name, "Play through radio", string.IsNullOrEmpty( icon ) ? "ui/icons/icon-ability-2.png" : icon, () => Player.PlayVoiceCmd( command ) );
		}
	}
}
