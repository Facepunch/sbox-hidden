using Sandbox;
using System;
using System.Collections.Generic;

namespace HiddenGamemode
{
	public class LightFlickers
	{
		private struct LightFlicker
		{
			public SpotLightEntity Entity;
			public float StartBrightness;
			public float EndTime;
		}

		private List<LightFlicker> _flickers = new();

		public void Add( SpotLightEntity entity, float flickerTime = 2f)
		{
			var index = _flickers.FindIndex( ( i ) => i.Entity == entity );

			if ( index >= 0 )
			{
				// Remove any existing flickers for this entity.
				var flicker = _flickers[index];
				flicker.Entity.Brightness = flicker.StartBrightness;
				_flickers.RemoveAt( index );
			}

			_flickers.Add( new LightFlicker
			{
				Entity = entity,
				StartBrightness = entity.Brightness,
				EndTime = Time.Now + flickerTime
			} );
		}

		public void OnTick()
		{
			var currentTime = Time.Now;

			for (var i = _flickers.Count - 1; i >= 0; i--)
			{
				var flicker = _flickers[i];

				if ( !flicker.Entity.IsValid() )
				{
					_flickers.RemoveAt( i );
					continue;
				}

				if ( currentTime < flicker.EndTime )
				{
					flicker.Entity.Brightness = flicker.StartBrightness * MathF.Abs( Noise.Perlin( currentTime * 5f, 0f, 0f ) );
				}
				else
				{
					flicker.Entity.Brightness = flicker.StartBrightness;
					_flickers.RemoveAt( i );
				}
			}
		}
	}
}
