
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
			RootPanel.AddChild<HitIndicator>();
			RootPanel.AddChild<ChatBox>();
			RootPanel.AddChild<Scoreboard>();
			RootPanel.AddChild<LoadingScreen>();

			HealthPostProcessing = new();
			ImmersionPostProcessing = new();

			PostProcess.Add( ImmersionPostProcessing );
			PostProcess.Add( HealthPostProcessing );
		}

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
				ImmersionPostProcessing.Contrast.Enabled = false;
				ImmersionPostProcessing.Saturate.Enabled = false;
				ImmersionPostProcessing.Brightness.Enabled = false;
				ImmersionPostProcessing.Vignette.Enabled = false;

				ImmersionPostProcessing.LensDistortion.Enabled = true;
				ImmersionPostProcessing.LensDistortion.K1 = 0.1f;
				ImmersionPostProcessing.LensDistortion.K2 = -0.1f;
			}
			else
			{
				ImmersionPostProcessing.Contrast.Enabled = true;
				ImmersionPostProcessing.Contrast.Contrast = 1.1f;

				ImmersionPostProcessing.Brightness.Enabled = true;
				ImmersionPostProcessing.Brightness.Multiplier = 0.9f;

				ImmersionPostProcessing.Saturate.Enabled = true;
				ImmersionPostProcessing.Saturate.Amount = 0.85f;

				ImmersionPostProcessing.LensDistortion.Enabled = true;
				ImmersionPostProcessing.LensDistortion.K1 = 0.02f;
				ImmersionPostProcessing.LensDistortion.K2 = -0.02f;

				ImmersionPostProcessing.Vignette.Enabled = true;
				ImmersionPostProcessing.Vignette.Intensity = 1f;
				ImmersionPostProcessing.Vignette.Color = Color.Black;
				ImmersionPostProcessing.Vignette.Smoothness = 2f;
				ImmersionPostProcessing.Vignette.Roundness = 1.6f;
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
