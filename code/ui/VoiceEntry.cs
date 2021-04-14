using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace HiddenGamemode
{
	public class VoiceEntry : Panel
	{
		public Action<VoiceEntry> OnDeleteCallback { get; set; }
		public Player Player { get; private set; }

		public readonly Label Name;
		public readonly Image Avatar;

		private float _voiceLevel = 0;

		public VoiceEntry()
		{
			Avatar = Add.Image( "", "avatar" );
			Name = Add.Label( "...", "name" );
		}

		public void Update( Player player )
		{
			Player = player;
			Name.Text = player.Name;
			Avatar.SetTexture( $"avatar:{player.SteamId}" );
		}

		public override void Tick()
		{
			base.Tick();

			if ( IsDeleting )
				return;

			if ( !Player.IsValid() )
			{
				Remove();
				return;
			}

			Name.Text = Player.Name;

			_voiceLevel = _voiceLevel.LerpTo( Player.VoiceLevel, Time.Delta * 10.0f );

			Name.Style.FontColor = Color.Lerp( Color.White.WithAlpha( 0.75f ), Color.FromBytes( 192, 251, 46 ), _voiceLevel * 10.0f );
			Name.Style.Dirty();

			var scale = 1.0f - (0.25f * (1.0f - _voiceLevel));

			var transform = new PanelTransform();
			transform.AddScale( scale );

			Avatar.Style.Transform = transform;
			Avatar.Style.Dirty();

			Style.Dirty();

			if ( Player.VoiceUsed && (Player.LastVoiceTime + 0.5f) < Time.Now )
			{
				Remove();
				return;
			}
		}

		private void Remove()
		{
			OnDeleteCallback?.Invoke(this);
			Delete();
		}
	}
}

