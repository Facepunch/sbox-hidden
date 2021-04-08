
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace HiddenGamemode
{
	public class Abilities : Panel
	{
		public Panel Sense;
		public Panel Scream;

		public Abilities()
		{
			Sense = MakeAbilityPanel( "sense" );
			Scream = MakeAbilityPanel( "scream" );
		}

		public override void Tick()
		{
			if ( Sandbox.Player.Local is not Player player ) return;

			if ( player.Team is not HiddenTeam ) return;

			UpdateAbility( Sense, player.Sense );
			UpdateAbility( Scream, player.Scream );
		}

		private Panel MakeAbilityPanel( string className)
		{
			var panel = Add.Panel( $"ability {className}" );
			panel.Add.Panel( $"icon {className}" );
			return panel;
		}

		private void UpdateAbility( Panel panel, BaseAbility ability )
		{
			panel.SetClass( "hidden", ability == null );

			if ( ability == null ) return;

			panel.SetClass( "usable", ability.IsUsable() );
		}
	}
}
