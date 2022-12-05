using Sandbox;
using Sandbox.UI;
using System;

namespace Facepunch.Hidden
{
	[UseTemplate]
	public partial class RadialMenuItem : Panel
	{
		public bool IsSelected { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public string IconPath { get; private set; }
		public Action OnSelected { get; set; }
		public Image Icon { get; set; }

		public RadialMenuItem() { }

		public void SetIcon( string icon )
		{
			Icon?.SetTexture( icon );
			IconPath = icon;
		}

		public override void OnLayout( ref Rect layoutRect )
		{
			base.OnLayout( ref layoutRect );

			var halfWidth = layoutRect.Width / 2f;
			var halfHeight = layoutRect.Height / 2f;

			layoutRect.Left -= halfWidth;
			layoutRect.Top -= halfHeight;
			layoutRect.Right -= halfWidth;
			layoutRect.Bottom -= halfHeight;
		}

		protected override void PostTemplateApplied()
		{
			BindClass( "selected", () => IsSelected );

			SetIcon( IconPath );

			base.PostTemplateApplied();
		}
	}
}
