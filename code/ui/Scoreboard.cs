using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace HiddenGamemode
{
	[UseTemplate]
	public class Scoreboard : Panel
	{
		Dictionary<Client, ScoreboardEntry> Entries = new();
		
		public Panel HiddenSection { get; set; }
		public Panel IrisSection { get; set; }

		public override void Tick()
		{
			base.Tick();

			SetClass("open", Input.Down(InputButton.Score));

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
			return client.GetInt("Team", 2) == 1 ? HiddenSection : IrisSection;
		}
	}
}
