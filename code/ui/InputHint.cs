using Sandbox;
using Sandbox.UI;
using System;

namespace Facepunch.Hidden
{
	[UseTemplate]
	public partial class InputHint : Panel
	{
		public Image Glyph { get; set; }
		public InputButton Button { get; set; }
		public string Text { get; set; }
		public Label ActionLabel { get; set; }

		protected bool IsSet = false;

		public InputHint()
		{
			BindClass( "noaction", () => string.IsNullOrEmpty( Text ) );
		}

		public override void SetProperty( string name, string value )
		{
			base.SetProperty( name, value );

			if ( name == "btn" )
			{
				SetButton( Enum.Parse<InputButton>( value, true ) );
			}
		}

		public void SetButton( InputButton button )
		{
			Button = button;
			IsSet = true;
		}

		public override void SetContent( string value )
		{
			base.SetContent( value );

			ActionLabel.SetText( value );
			Text = value;
		}

		public override void Tick()
		{
			base.Tick();

			if ( IsSet )
			{
				var glyphTexture = Input.GetGlyph( Button, InputGlyphSize.Medium, GlyphStyle.Light.WithSolidABXY().WithNeutralColorABXY() );

				if ( glyphTexture is null )
					return;

				Glyph.Texture = glyphTexture;


				if ( glyphTexture.Width > glyphTexture.Height )
				{
					Glyph.Style.Width = Length.Pixels( 64f );
					Glyph.Style.Height = Length.Pixels( 32f );
				}
				else
				{
					Glyph.Style.Width = Length.Pixels( 32f );
					Glyph.Style.Height = Length.Pixels( 32f );
				}
			}
		}
	}
}
