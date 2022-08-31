
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
	[Library]
	public partial class Hud : HudEntity<RootPanel>
	{
		private StandardPostProcess HealthPostProcessing { get; set; }
		private StandardPostProcess ImmersionPostProcessing { get; set; }

		public Hud()
		{
			if ( !IsClient )
				return;

			RootPanel.StyleSheet.Load( "/ui/Hud.scss" );

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

			PostProcess.Add( ImmersionPostProcessing );
			PostProcess.Add( HealthPostProcessing );
		}

		private Color OverlayColor { get; set; } = Color.Orange;
		private float BlurAmount { get; set; } = 0f;

		[Event.Tick.Client]
		private void ClientTick()
		{
			if ( Local.Pawn is not Player player ) return;

			var isHiddenTeam = player.Team is HiddenTeam;

			if ( isHiddenTeam )
			{
				ImmersionPostProcessing.ChromaticAberration.Enabled = true;
				ImmersionPostProcessing.ChromaticAberration.Offset = new Vector3( 0.002f, 0f, 0.002f );
			}
			else
			{
				ImmersionPostProcessing.ChromaticAberration.Enabled = false;
			}

			ImmersionPostProcessing.Sharpen.Enabled = true;
			ImmersionPostProcessing.Sharpen.Strength = 0.1f;

			if ( isHiddenTeam )
			{
				ImmersionPostProcessing.Saturate.Enabled = false;
				ImmersionPostProcessing.Vignette.Enabled = false;
				ImmersionPostProcessing.FilmGrain.Enabled = false;

				ImmersionPostProcessing.LensDistortion.Enabled = true;
				ImmersionPostProcessing.LensDistortion.K1 = 0.1f;
				ImmersionPostProcessing.LensDistortion.K2 = -0.1f;

				ImmersionPostProcessing.ColorOverlay.Enabled = true;

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

				ImmersionPostProcessing.ColorOverlay.Color = OverlayColor;
				ImmersionPostProcessing.ColorOverlay.Mode = StandardPostProcess.ColorOverlaySettings.OverlayMode.Multiply;
				ImmersionPostProcessing.ColorOverlay.Amount = 0.8f;

				ImmersionPostProcessing.Blur.Enabled = true;
				ImmersionPostProcessing.Blur.Strength = BlurAmount;
			}
			else
			{
				ImmersionPostProcessing.ColorOverlay.Enabled = false;
				ImmersionPostProcessing.Blur.Enabled = false;

				ImmersionPostProcessing.Saturate.Enabled = true;
				ImmersionPostProcessing.Saturate.Amount = 0.9f;

				ImmersionPostProcessing.LensDistortion.Enabled = true;
				ImmersionPostProcessing.LensDistortion.K1 = 0.02f;
				ImmersionPostProcessing.LensDistortion.K2 = -0.02f;

				ImmersionPostProcessing.Vignette.Enabled = true;
				ImmersionPostProcessing.Vignette.Intensity = 1f;
				ImmersionPostProcessing.Vignette.Color = Color.Black;
				ImmersionPostProcessing.Vignette.Smoothness = 2f;
				ImmersionPostProcessing.Vignette.Roundness = 1.7f;

				ImmersionPostProcessing.FilmGrain.Enabled = true;
				ImmersionPostProcessing.FilmGrain.Response = 0.6f;
				ImmersionPostProcessing.FilmGrain.Intensity = 0.5f;
			}

			if ( player.CameraMode is SpectateCamera camera && camera.CCTVEntity.IsValid() )
			{
				ImmersionPostProcessing.Saturate.Amount = 0f;
				ImmersionPostProcessing.LensDistortion.K1 = 0.05f;
				ImmersionPostProcessing.LensDistortion.K2 = -0.05f;
				ImmersionPostProcessing.FilmGrain.Response = 0.3f;
				ImmersionPostProcessing.FilmGrain.Intensity = 0.5f;
			}

			var healthScale = (0.4f / 100f) * player.Health;
			HealthPostProcessing.Saturate.Enabled = true;
			HealthPostProcessing.Saturate.Amount = 0.6f + healthScale;

			HealthPostProcessing.Vignette.Enabled = true;
			HealthPostProcessing.Vignette.Intensity = 0.8f - healthScale * 2f;
			HealthPostProcessing.Vignette.Color = Color.Red;
			HealthPostProcessing.Vignette.Smoothness = 4f;
			HealthPostProcessing.Vignette.Roundness = 2f;
		}
	}
}
