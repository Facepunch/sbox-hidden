using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	[UseTemplate]
	public partial class SimpleRadialMenu : Panel
	{
		public virtual InputButton Button => InputButton.Score;

		public List<RadialMenuItem> Items { get; private set; }
		public RadialMenuItem Hovered { get; private set; }
		public Panel ItemContainer { get; set; }
		public string Title => Hovered?.Title ?? string.Empty;
		public string Description => Hovered?.Description ?? string.Empty;
		public Panel Dot { get; set; }

		private TimeSince LastCloseTime { get; set; }
		private Vector2 VirtualMouse { get; set; }
		private bool IsOpen { get; set; }

		public void Initialize()
		{
			Items ??= new();

			foreach ( var item in Items )
			{
				item.Delete();
			}

			Items.Clear();

			Populate();
		}

		public void AddAction( string title, string description, string icon, Action callback )
		{
			var item = ItemContainer.AddChild<RadialMenuItem>();
			item.Title = title;
			item.Description = description;
			item.SetIcon( icon );
			item.OnSelected = callback;
			Items.Add( item );
		}

		[Event.BuildInput]
		public void BuildInput()
		{
			var shouldOpen = ShouldOpen();

			if ( Input.Pressed( Button ) && shouldOpen )
			{
				VirtualMouse = Screen.Size * 0.5f;
				IsOpen = true;
			}

			if ( Input.Released( Button ) || !shouldOpen )
			{
				IsOpen = false;
			}

			if ( IsOpen )
			{
				VirtualMouse += new Vector2( Input.AnalogLook.Direction.y, Input.AnalogLook.Direction.z ) * -500f;

				var lx = VirtualMouse.x - Box.Left;
				var ly = VirtualMouse.y - Box.Top;

				RadialMenuItem closestItem = null;
				var closestDistance = 0f;

				if ( VirtualMouse.Distance( Screen.Size * 0.5f ) >= Box.Rect.Size.x * 0.1f )
				{
					foreach ( var item in Items )
					{
						var distance = item.Box.Rect.Center.Distance( VirtualMouse );

						if ( closestItem == null || distance < closestDistance )
						{
							closestDistance = distance;
							closestItem = item;
						}
					}
				}

				Hovered = closestItem;

				Dot.Style.Left = Length.Pixels( lx * ScaleFromScreen );
				Dot.Style.Top = Length.Pixels( ly * ScaleFromScreen );

				if ( Hovered != null && Input.Down( InputButton.PrimaryAttack ) )
				{
					Hovered.OnSelected?.Invoke();
					LastCloseTime = 0f;
					IsOpen = false;
				}

				Input.AnalogLook = Angles.Zero;
			}

			if ( IsOpen || LastCloseTime < 0.1f )
			{
				Input.ClearButton( InputButton.PrimaryAttack );
				Input.ClearButton( InputButton.SecondaryAttack );
			}
		}

		public virtual void Populate()
		{

		}

		public override void Tick()
		{
			foreach ( var item in Items )
			{
				item.IsSelected = (Hovered == item);

				var fItemCount = (float)Items.Count;
				var maxItemScale = 1f;
				var minItemScale = 0.9f - fItemCount.Remap( 4f, 10f, 0f, 0.4f );
				var distanceToMouse = item.Box.Rect.Center.Distance( VirtualMouse );
				var distanceToScale = distanceToMouse.Remap( 0f, item.Box.Rect.Size.Length * 1.5f, maxItemScale, minItemScale ).Clamp( minItemScale, maxItemScale );

				var tx = new PanelTransform();
				tx.AddScale( distanceToScale );
				item.Style.Transform = tx;

				item.Style.ZIndex = item.IsSelected ? 2 : 0;
			}

			base.Tick();
		}

		protected override void PostTemplateApplied()
		{
			BindClass( "hidden", IsHidden );
			Initialize();
			base.PostTemplateApplied();
		}

		protected virtual bool ShouldOpen()
		{
			return true;
		}

		protected override void FinalLayoutChildren()
		{
			var radius = Box.Rect.Size.x * 0.5f;
			var center = Box.Rect.WithoutPosition.Center;

			for ( var i = 0; i < Items.Count; i++ )
			{
				var theta = (i * 2f * Math.PI / Items.Count) - Math.PI;
				var x = (float)Math.Sin( theta ) * radius;
				var y = (float)Math.Cos( theta ) * radius;
				var item = Items[i];

				item.Style.Left = Length.Pixels( (center.x + x) * ScaleFromScreen );
				item.Style.Top = Length.Pixels( (center.y + y) * ScaleFromScreen );
			}

			base.FinalLayoutChildren();
		}

		private bool IsHidden() => !IsOpen;
	}
}
