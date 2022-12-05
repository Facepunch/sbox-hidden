using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	public static partial class Util
	{
		public static Draw2D Draw { get; internal set; } = new();
	}

	public class Draw2D
	{
		internal Draw2D()
		{
			Reset();
		}

		public Draw2D Reset()
		{
			FontFamily = "Roboto";
			FontSize = 20f;
			FontWeight = 400f;
			VertexList ??= new();
			VertexList.Clear();
			Attributes = new RenderAttributes();
			BlendMode = BlendMode.Normal;
			Texture = Texture.White;

			return this;
		}

		public RenderAttributes Attributes { get; set; }
		public string FontFamily { get; set; }
		public float FontSize { get; set; }
		public float FontWeight { get; set; }
		public Color Color { get; set; }

		public Texture Texture
		{
			set
			{
				if ( Attributes == null ) return;
				Attributes.Set( "Texture", value );
			}
		}

		public BlendMode BlendMode
		{
			set
			{
				if ( Attributes == null ) return;
				Attributes.SetCombo( "D_BLENDMODE", (int)value );
			}
		}

		public void Box( Rect rect, Vector4 corners = default )
		{
			Graphics.DrawRoundedRectangle( rect, Color, corners );
		}

		public void BoxWithBorder( in Rect rect, float borderSize, in Color borderColor, in Vector4 corners = default )
		{
			Graphics.DrawRoundedRectangle( rect, Color, corners, new Vector4( borderSize ), borderColor );
		}

		public void Box( Rect rect, Color color, Vector4 corners = default )
		{
			Color = color;
			Box( rect, corners );
		}

		public void BoxWithBorder( Rect rect, Color color, float borderSize, Color borderColor, Vector4 corners = default )
		{
			Graphics.DrawRoundedRectangle( rect, color, corners, new Vector4( borderSize ), borderColor );
		}

		public void Quad( Rect rect, Color color )
		{
			Graphics.DrawQuad( rect, Material.UI.Basic, color, Attributes );
		}

		public List<Vertex> VertexList = new List<Vertex>();

		public void AddVertex( in Vertex v )
		{
			VertexList.Add( v );
		}

		public void AddVertex( in Vector2 v, in Color color )
		{
			AddVertex( new Vertex { Position = v, Color = color } );
		}

		public void AddVertex( in Vector2 v, in Color color, in Vector2 uv )
		{
			AddVertex( new Vertex { Position = v, Color = color, TexCoord0 = uv } );
		}

		public void AddVertex( in Vector2 v, in Vector2 uv )
		{
			AddVertex( new Vertex { Position = v, Color = Color, TexCoord0 = uv } );
		}

		public void MeshStart()
		{
			VertexList.Clear();
		}

		public void MeshEnd()
		{
			var span = new Span<Vertex>( VertexList.ToArray() );
			Graphics.Draw( span, VertexList.Count, Material.UI.Basic, Attributes );
			VertexList.Clear();
		}

		public void Clip( Rect? rect )
		{
			if ( !rect.HasValue )
			{
				Attributes.SetCombo( "D_SCISSOR", 0 );
				return;
			}

			Attributes.Set( "ScissorRect", rect.Value.ToVector4() );
			Attributes.SetCombo( "D_SCISSOR", 1 );
		}

		public void Circle( in Vector2 center, float radius, int points = 32 )
		{
			CircleEx( center, radius, 0, points: points );
		}

		public void Ring( in Vector2 center, float radius, float holeRadius, int points = 32 )
		{
			CircleEx( center, radius, holeRadius, points: points );
		}

		public void CircleEx( in Vector2 center, float outer, float inner, int points = 32, float startAngle = 0.0f, float endAngle = 360.0f, float uv = 0 )
		{
			MeshStart();
			var pi2 = MathF.PI * 2.0f;

			startAngle = startAngle.NormalizeDegrees().DegreeToRadian();
			endAngle = endAngle.NormalizeDegrees().DegreeToRadian();

			while ( endAngle <= startAngle ) endAngle += pi2;

			float totalAngle = (endAngle - startAngle) % (pi2 + 0.01f);
			if ( totalAngle <= 0 ) return;

			var step = pi2 / points;

			for ( float i = startAngle; i < endAngle; i += step )
			{
				var r0 = i;
				var r1 = i + step;

				if ( r1 > endAngle ) r1 = endAngle;

				r0 += MathF.PI;
				r1 += MathF.PI;

				var d0 = new Vector2( MathF.Sin( -r0 ), MathF.Cos( -r0 ) );
				var d1 = new Vector2( MathF.Sin( -r1 ), MathF.Cos( -r1 ) );

				var u0 = (d0) / 2 + 0.5f;
				var u1 = (d1) / 2 + 0.5f;
				var u2 = (d0 * inner / outer) / 2 + 0.5f;
				var u3 = (d1 * inner / outer) / 2 + 0.5f;

				if ( uv > 0 )
				{
					u0 = new( (r0 - MathF.PI - startAngle) * uv / pi2, 0 );
					u1 = new( (r1 - MathF.PI - startAngle) * uv / pi2, 0 );
					u2 = u0.WithY( 1 );
					u3 = u1.WithY( 1 );
				}

				AddVertex( center + d0 * outer, Color, u0 );
				AddVertex( center + d1 * outer, Color, u1 );
				AddVertex( center + d0 * inner, Color, u2 );

				if ( inner > 0 )
				{
					AddVertex( center + d1 * outer, Color, u1 );
					AddVertex( center + d1 * inner, Color, u3 );
					AddVertex( center + d0 * inner, Color, u2 );
				}
			}

			MeshEnd();
		}

		public void Line( in float t0, in Vector2 p0, in float t1, in Vector2 p1 )
		{
			MeshStart();
			var forward = p1 - p0;
			var right = forward.Perpendicular.Normal * -0.5f;

			var v0 = p0 + right * t0;
			var v1 = p0 + forward + right * t1;
			var v2 = p0 + forward - right * t1;
			var v3 = p0 - right * t0;

			AddVertex( v0, new Vector2( 0, 0 ) );
			AddVertex( v1, new Vector2( 1, 0 ) );
			AddVertex( v3, new Vector2( 0, 1 ) );

			AddVertex( v1, new Vector2( 1, 0 ) );
			AddVertex( v2, new Vector2( 1, 1 ) );
			AddVertex( v3, new Vector2( 0, 1 ) );
			MeshEnd();
		}

		public void Line( in float t0, in Vector2 p0, in Vector2 p1 ) => Line( t0, p0, t0, p1 );

		public void SetFont( string name, float size = 8, int weight = 400, bool italic = false, bool sizeInPixels = false )
		{
			FontFamily = name;
			FontSize = size;
			FontWeight = weight;

			if ( sizeInPixels )
				FontSize = size * 72.0f / 96.0f;
		}

		public Rect DrawIcon( Rect rect, string iconName, float pixelHeight, TextFlag alignment = TextFlag.Center )
		{
			return Graphics.DrawIcon( rect, iconName, Color, pixelHeight, alignment );
		}

		public Rect DrawText( in Rect position, string text, TextFlag flags = TextFlag.Center )
		{
			return Graphics.DrawText( position, text, Color, FontFamily, FontSize, FontWeight, flags );
		}

		public Rect MeasureText( in Rect position, string text, TextFlag flags = TextFlag.Center )
		{
			return Graphics.MeasureText( position, text, FontFamily, FontSize, FontWeight, flags );
		}
	}
}
