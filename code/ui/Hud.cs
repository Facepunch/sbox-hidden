
using Sandbox;
using Sandbox.Effects;
using Sandbox.UI;

namespace Facepunch.Hidden
{
	[Library]
	public partial class Hud : HudEntity<RootPanel>
	{
		private ScreenEffects HealthPostProcessing { get; set; }
		private ScreenEffects ImmersionPostProcessing { get; set; }
		private Panel Overlay { get; set; }

		public Hud()
		{
			if ( !IsClient )
				return;

			RootPanel.StyleSheet.Load( "/ui/Hud.scss" );

			Overlay = RootPanel.Add.Panel( "overlay" );

			var rightPanel = RootPanel.Add.Panel( "hud_right" );
			rightPanel.AddChild<WeaponList>();

			RootPanel.AddChild<RoundInfo>();
			RootPanel.AddChild<Vitals>();
			RootPanel.AddChild<Ammo>();
			RootPanel.AddChild<VoiceList>();
			RootPanel.AddChild<Nameplates>();
			RootPanel.AddChild<DamageIndicator>();
			RootPanel.AddChild<InputHints>();
			RootPanel.AddChild<CCTVViewer>();
			RootPanel.AddChild<ChatBox>();
			RootPanel.AddChild<Scoreboard>();

			var centerPanel = RootPanel.Add.Panel( "hud_center" );
			centerPanel.AddChild<RadialVoiceMenu>();

			HealthPostProcessing = new();
			ImmersionPostProcessing = new();
		}

		private Color OverlayColor { get; set; } = Color.Orange;
		private float BlurAmount { get; set; } = 0f;

		[Event.Tick.Client]
		private void ClientTick()
		{
			if ( Local.Pawn is not Player player )
				return;

			Camera.Current?.AddHook( ImmersionPostProcessing );
			Camera.Current?.AddHook( HealthPostProcessing );

			var isHiddenTeam = player.Team is HiddenTeam;

			if ( isHiddenTeam )
			{
				ImmersionPostProcessing.ChromaticAberration.Scale = 0.75f;
				ImmersionPostProcessing.ChromaticAberration.Offset = new Vector3( 0.002f, 0f, 0.002f );
			}
			else
			{
				ImmersionPostProcessing.ChromaticAberration.Scale = 0f;
			}

			Overlay.SetClass( "hidden", !isHiddenTeam );

			ImmersionPostProcessing.Sharpen = 0.1f;

			if ( isHiddenTeam )
			{
				ImmersionPostProcessing.Saturation = 1f;
				ImmersionPostProcessing.FilmGrain.Intensity = 0f;

				ImmersionPostProcessing.Vignette.Intensity = 0.5f;
				ImmersionPostProcessing.Vignette.Color = Color.Red.Darken( 0.3f ).WithAlpha( 0.9f );
				ImmersionPostProcessing.Vignette.Smoothness = 0.9f;
				ImmersionPostProcessing.Vignette.Roundness = 0.5f;

				/*
				ImmersionPostProcessing.LensDistortion.Enabled = true;
				ImmersionPostProcessing.LensDistortion.K1 = 0.1f;
				ImmersionPostProcessing.LensDistortion.K2 = -0.1f;

				ImmersionPostProcessing.ColorOverlay.Enabled = true;
				*/

				if ( player.IsSenseActive )
				{
					OverlayColor = Color.Lerp( OverlayColor, Color.Red, Time.Delta * 4f );
					BlurAmount = BlurAmount.LerpTo( 0f, Time.Delta * 4f );
				}
				else
				{
					OverlayColor = Color.Lerp( OverlayColor, Color.Orange, Time.Delta * 4f );
					BlurAmount = BlurAmount.LerpTo( 0f, Time.Delta * 4f );
				}

				/*
				ImmersionPostProcessing.ColorOverlay.Color = OverlayColor;
				ImmersionPostProcessing.ColorOverlay.Mode = StandardPostProcess.ColorOverlaySettings.OverlayMode.Multiply;
				ImmersionPostProcessing.ColorOverlay.Amount = 0.8f;
				*/

				/*
				ImmersionPostProcessing.Blur.Enabled = true;
				ImmersionPostProcessing.Blur.Strength = BlurAmount;
				*/
			}
			else
			{
				/*
				ImmersionPostProcessing.ColorOverlay.Enabled = false;
				ImmersionPostProcessing.Blur.Enabled = false;
				*/

				ImmersionPostProcessing.Saturation = 0.9f;

				/*
				ImmersionPostProcessing.LensDistortion.Enabled = true;
				ImmersionPostProcessing.LensDistortion.K1 = 0.02f;
				ImmersionPostProcessing.LensDistortion.K2 = -0.02f;
				*/

				ImmersionPostProcessing.Vignette.Intensity = 0.7f;
				ImmersionPostProcessing.Vignette.Color = Color.Black.WithAlpha( 0.5f );
				ImmersionPostProcessing.Vignette.Smoothness = 0.9f;
				ImmersionPostProcessing.Vignette.Roundness = 0.7f;

				ImmersionPostProcessing.FilmGrain.Response = 0.3f;
				ImmersionPostProcessing.FilmGrain.Intensity = 0.05f;
			}

			if ( player.IsSpectator && !isHiddenTeam )
			{
				ImmersionPostProcessing.Saturation = 0f;
				/*
				ImmersionPostProcessing.LensDistortion.K1 = 0.05f;
				ImmersionPostProcessing.LensDistortion.K2 = -0.05f;
				*/
				ImmersionPostProcessing.FilmGrain.Response = 0.2f;
				ImmersionPostProcessing.FilmGrain.Intensity = 0.4f;

				HealthPostProcessing.Vignette.Intensity = 0f;
			}
			else
			{
				var healthScale = (0.4f / 100f) * player.Health;
				HealthPostProcessing.Saturation = 0.6f + healthScale;

				HealthPostProcessing.Vignette.Intensity = 0.8f - healthScale * 2f;
				HealthPostProcessing.Vignette.Color = Color.Red.Darken( 0.8f ).WithAlpha( 0.5f );
				HealthPostProcessing.Vignette.Smoothness = 0.9f;
				HealthPostProcessing.Vignette.Roundness = 0.5f;
			}
		}
	}
}
