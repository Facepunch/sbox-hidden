using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	[GameResource( "Radio Command", "radio", "A radio command used by I.R.I.S." )]
	public class RadioCommandResource : GameResource
	{
		private static Dictionary<string, RadioCommandResource> LookupByName { get; set; } = new();

		public static RadioCommandResource FindByName( string name )
		{
			if ( LookupByName.TryGetValue( name, out var value ) )
			{
				return value;
			}

			return null;
		}

		[Property] public string Text { get; set; }
		[Property] public SoundEvent Sound { get; set; }
		[Property] public SoundEvent ProximitySound { get; set; }
		[Property] public float ProximityDistance { get; set; } = 1024f;

		protected override void PostLoad()
		{
			var nameWithoutExtension = ResourceName.Replace( ".radio", "" );
			LookupByName[nameWithoutExtension] = this;
			base.PostLoad();
		}
	}
}
