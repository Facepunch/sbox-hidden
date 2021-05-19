
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace HiddenGamemode
{
	public class Abilities : Panel
	{
		public class AbilityInfo
		{
			public Panel Panel;
			public Panel CooldownPanel;
			public Label CooldownLabel;
		}

		public AbilityInfo Sense;
		public AbilityInfo Scream;

		public Abilities()
		{
			Sense = MakeAbilityPanel( "sense" );
			Scream = MakeAbilityPanel( "scream" );
		}

		public override void Tick()
		{
			if ( Local.Pawn is not Player player ) return;

			if ( player.Team is not HiddenTeam ) return;

			SetClass( "hidden", player.LifeState != LifeState.Alive );

			UpdateAbility( Sense, player.Sense );
			UpdateAbility( Scream, player.Scream );
		}

		private AbilityInfo MakeAbilityPanel( string className )
		{
			var ability = new AbilityInfo();

			ability.Panel = Add.Panel( $"ability {className}" );
			ability.Panel.Add.Panel( $"icon {className}" );

			ability.CooldownPanel = ability.Panel.Add.Panel( "cooldown " );
			ability.CooldownLabel = ability.CooldownPanel.Add.Label( "0", "text " );

			return ability;
		}

		private void UpdateAbility( AbilityInfo info, BaseAbility ability )
		{
			if ( Local.Pawn is not Player player )
				return;

			info.Panel.SetClass( "hidden", ability == null );

			if ( ability == null ) return;

			var isUsable = ability.IsUsable( player );

			info.Panel.SetClass( "usable", isUsable );
			info.CooldownPanel.SetClass( "usable", isUsable );

			if ( isUsable )
				info.CooldownLabel.Text = ability.GetKeybind();
			else
				info.CooldownLabel.Text = ((int)ability.GetCooldownTimeLeft( player )).ToString();
		}
	}
}
