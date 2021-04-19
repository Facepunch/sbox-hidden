using Sandbox;
using System;

namespace HiddenGamemode
{
	partial class Player
	{
		public float FlashlightBattery { get; set; } = 100f;

		private Flashlight _flashlight;

		public bool HasFlashlightEntity
		{
			get
			{
				return (_flashlight != null && _flashlight.IsValid());
			}
		}

		public bool IsFlashlightOn
		{
			get
			{
				return (HasFlashlightEntity && _flashlight.Enabled);
			}
		}

		public void ToggleFlashlight()
		{
			ShowFlashlight( !IsFlashlightOn );
		}

		public void ShowFlashlight( bool shouldShow, bool playSounds = true )
		{
			if ( HasFlashlightEntity )
			{
				_flashlight.Enabled = false;
			}

			if ( ActiveChild is not Weapon weapon || !weapon.HasFlashlight )
				return;

			if ( shouldShow )
			{
				if ( !HasFlashlightEntity )
				{
					_flashlight = new Flashlight();
					_flashlight.Rot = EyeRot;
					_flashlight.SetParent( weapon, "muzzle" );
					_flashlight.Pos = Vector3.Zero;
				}
				else
				{
					// TODO: This is a weird hack to make sure the rotation is right.
					_flashlight.SetParent( null );
					_flashlight.Rot = EyeRot;
					_flashlight.SetParent( weapon, "muzzle" );
					_flashlight.Pos = Vector3.Zero;
					_flashlight.Enabled = true;
				}

				_flashlight.UpdateFromBattery( FlashlightBattery );
				_flashlight.Reset();

				if ( playSounds )
					PlaySound( "flashlight-on" );
			}
			else if ( playSounds )
			{
				PlaySound( "flashlight-off" );
			}
		}

		private void TickFlashlight()
		{
			if ( IsServer )
			{
				if ( _flashlight != null && _flashlight.IsValid() && _flashlight.Enabled )
				{
					FlashlightBattery = MathF.Max( FlashlightBattery - 10f * Time.Delta, 0f );

					using ( Prediction.Off() )
					{
						var shouldTurnOff = _flashlight.UpdateFromBattery( FlashlightBattery );

						if ( shouldTurnOff )
							ShowFlashlight( false );
					}
				}
				else
				{
					FlashlightBattery = MathF.Min( FlashlightBattery + 15f * Time.Delta, 100f );
				}
			}
		}
	}
}
