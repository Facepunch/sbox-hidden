using Sandbox.UI.Construct;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace HiddenGamemode
{
	public class Nameplate : Panel
	{
		public Label NameLabel;

		public Nameplate( Player player )
		{
			var client = player.GetClientOwner();
			NameLabel = Add.Label( client.Name );
		}
	}
}
