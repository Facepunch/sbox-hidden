using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Facepunch.Hidden
{
	[UseTemplate]
	public class Scoreboard : Panel
	{
		Dictionary<Client, ScoreboardEntry> Entries = new();
		
		public Panel HiddenSection { get; set; }
		public Panel IrisSection { get; set; }

		public static Scoreboard Current { get; private set; }

		public Scoreboard()
		{
			Current = this;
		}

		public bool IsOpen => Input.Down( InputButton.Score );

		public override void Tick()
		{
			base.Tick();

			SetClass( "open", IsOpen );

			if ( !IsVisible )
				return;

			foreach(Client cl in Client.All.Except(Entries.Keys))
			{
				ScoreboardEntry entry = new();
				Entries.Add(cl, entry);
				entry.UpdateFrom(cl);

			}

			foreach (Client cl in Entries.Keys.Except(Client.All))
			{
				if( Entries.TryGetValue(cl, out var entry))
				{
					entry.Delete();
					Entries.Remove(cl);
				}
			}

			var incorrectlyLocated = Entries.Where(kvp => kvp.Value.Parent != GetCorrectSection(kvp.Key)).ToList();

			foreach(var kvp in incorrectlyLocated)
				kvp.Value.Parent = GetCorrectSection(kvp.Key);
		}

		private Panel GetCorrectSection(Client client)
		{
			return client.GetInt( "Team", 2 ) == 1 ? HiddenSection : IrisSection;
		}
	}
}
