using Sandbox;

namespace Facepunch.Hidden;

public partial class InputHints
{

	[ClientRpc]
	public static void UpdateOnClient()
	{
		Current?.Update();
	}
}
