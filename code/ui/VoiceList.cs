using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;
using System.Linq;

namespace HiddenGamemode
{
	public class VoiceList : Panel
	{
		public VoiceList()
		{
			StyleSheet.Load( "/ui/VoiceList.scss" );

			Refresh();
		}

		private Dictionary<Player, VoiceEntry> _entries = new();

		private void Refresh()
		{
			foreach ( var v in Sandbox.Player.All )
			{
				if ( v is not Player player )
					continue;

				if ( player.Team is HiddenTeam && !player.IsLocalPlayer )
					continue;

				if ( !player.VoiceUsed )
					continue;

				if ( player.LastVoiceTime + 0.5f < Time.Now )
					continue;

				if ( _entries.TryGetValue(player, out var _ ))
					continue;

				var entry = AddChild<VoiceEntry>();
				entry.OnDeleteCallback += OnVoiceEntryDeleted;
				entry.Update( player );
				_entries[player] = entry;
			}
		}

		public override void Tick()
		{
			base.Tick();

			Refresh();
		}

		private void OnVoiceEntryDeleted(VoiceEntry entry)
		{
			_entries.Remove( entry.Player );
		}
	}
}
