using Sandbox;

namespace Facepunch.Hidden;

public partial class WeaponList
{
	[ClientRpc]
	public static void Expand( float duration )
    {
		if ( Instance != null )
        {
			Instance.RemainOpenUntil = duration;
		}
	}
}
