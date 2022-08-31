using Sandbox;
using System;

namespace Facepunch.Hidden
{
	partial class Player
	{
		[Net, Local, Predicted] public float FlashlightBattery { get; set; } = 100f;

		private Flashlight WorldFlashlight;
		private Flashlight ViewFlashlight;

		public bool HasFlashlightEntity
		{
			get
			{
				if ( IsLocalPawn )
				{
					return (ViewFlashlight != null && ViewFlashlight.IsValid());
				}

				return (WorldFlashlight != null && WorldFlashlight.IsValid());
			}
		}

		public bool IsFlashlightOn
		{
			get
			{
				if ( IsLocalPawn )
					return (HasFlashlightEntity && ViewFlashlight.Enabled);

				return (HasFlashlightEntity && WorldFlashlight.Enabled);
			}
		}

		public void ToggleFlashlight()
		{
			ShowFlashlight( !IsFlashlightOn );
		}

		public void ShowFlashlight( bool shouldShow, bool playSounds = true )
		{
			if ( IsFlashlightOn )
			{
				if ( IsServer )
					WorldFlashlight.Enabled = false;
				else
					ViewFlashlight.Enabled = false;
			}

			if ( IsServer && IsFlashlightOn != shouldShow )
			{
				ShowFlashlightLocal( To.Single( this ), shouldShow );
			}

			if ( ActiveChild is not Weapon weapon || !weapon.HasFlashlight )
				return;

			if ( shouldShow )
			{
				if ( !HasFlashlightEntity )
				{
					if ( IsServer )
					{
						WorldFlashlight = new Flashlight();
						WorldFlashlight.EnableHideInFirstPerson = true;
						WorldFlashlight.LocalRotation = EyeRotation;
						WorldFlashlight.SetParent( weapon, "muzzle" );
						WorldFlashlight.LocalPosition = Vector3.Zero;
					}
					else
					{
						ViewFlashlight = new Flashlight();
						ViewFlashlight.EnableViewmodelRendering = true;
						ViewFlashlight.Rotation = EyeRotation;
						ViewFlashlight.Position = EyePosition + EyeRotation.Forward * 10f;
					}
				}
				else
				{
					if ( IsServer )
					{
						WorldFlashlight.SetParent( null );
						WorldFlashlight.LocalRotation = EyeRotation;
						WorldFlashlight.SetParent( weapon, "muzzle" );
						WorldFlashlight.LocalPosition = Vector3.Zero;
						WorldFlashlight.Enabled = true;
					}
					else
					{
						ViewFlashlight.Enabled = true;
					}
				}

				if ( IsServer )
				{
					WorldFlashlight.UpdateFromBattery( FlashlightBattery );
					WorldFlashlight.Reset();
				}
				else
				{
					ViewFlashlight.UpdateFromBattery( FlashlightBattery );
					ViewFlashlight.Reset();
				}

				if ( IsServer && playSounds )
					PlaySound( "flashlight-on" );
			}
			else if ( IsServer && playSounds )
			{
				PlaySound( "flashlight-off" );
			}
		}

		[ClientRpc]
		private void ShowFlashlightLocal( bool shouldShow )
		{
			ShowFlashlight( shouldShow );
		}

		private void TickFlashlight()
		{
			if ( Input.Released(InputButton.Flashlight) )
			{
				using ( Prediction.Off() )
					ToggleFlashlight();
			}

			if ( IsFlashlightOn )
			{
				FlashlightBattery = MathF.Max( FlashlightBattery - 10f * Time.Delta, 0f );

				using ( Prediction.Off() )
				{
					if ( IsServer )
					{
						var shouldTurnOff = WorldFlashlight.UpdateFromBattery( FlashlightBattery );

						if ( shouldTurnOff )
							ShowFlashlight( false, false );
					}
					else
					{
						var viewFlashlightParent = ViewFlashlight.Parent;

						if ( ActiveChild is Weapon weapon && weapon.ViewModelEntity != null )
						{
							if ( viewFlashlightParent != weapon.ViewModelEntity )
							{
								ViewFlashlight.SetParent( weapon.ViewModelEntity, "muzzle" );
								ViewFlashlight.Rotation = EyeRotation;
								ViewFlashlight.LocalPosition = Vector3.Zero;
							}
						}
						else
						{
							if ( viewFlashlightParent != null )
								ViewFlashlight.SetParent( null );

							ViewFlashlight.Rotation = EyeRotation;
							ViewFlashlight.Position = EyePosition + EyeRotation.Forward * 80f;
						}

						var shouldTurnOff = ViewFlashlight.UpdateFromBattery( FlashlightBattery );

						if ( shouldTurnOff )
							ShowFlashlight( false, false );
					}
				}
			}
			else
			{
				FlashlightBattery = MathF.Min( FlashlightBattery + 15f * Time.Delta, 100f );
			}
		}
	}
}
