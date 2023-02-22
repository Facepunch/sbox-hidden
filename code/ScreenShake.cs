using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Hidden;

public abstract partial class ScreenShake
{
	internal static List<ScreenShake> List = new();

	internal static void Apply()
	{
		for ( int i = List.Count; i > 0; i-- )
		{
			var entry = List[i - 1];
			var keep = entry.Update();

			if ( !keep )
			{
				entry.OnRemove();
				List.RemoveAt( i - 1 );
			}
		}
	}

	internal static void Add( ScreenShake shake )
	{
		if ( Prediction.FirstTime )
		{
			List.Add( shake );
		}
	}

	protected virtual void OnRemove()
	{

	}

	[ClientRpc]
	public static void DoRandomShake( Vector3 position, float range = 512f, float intensity = 1f )
	{
		if ( Game.LocalPawn is not HiddenPlayer player ) return;

		var distance = player.Position.Distance( position );
		if ( distance >= range ) return;

		intensity = 1f + (intensity * 0.5f);

		var scale = distance.Remap( 0f, range, 1f, 0f );
		intensity *= scale;

		var shake = new Random( 1.5f, intensity );
		Add( shake );
	}

	public static void ClearAll()
	{
		List.Clear();
	}

	public abstract bool Update();
}
