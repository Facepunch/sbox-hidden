using Sandbox.UI.Construct;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace Facepunch.Hidden
{
	public class Nameplate : Panel
	{
		public Label NameLabel;

		public Nameplate( Player player )
		{
			NameLabel = Add.Label( player.Client.Name );
		}
	}
}
