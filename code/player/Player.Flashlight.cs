using Sandbox;
using System;

namespace HiddenGamemode
{
	partial class Player
	{
		[NetLocal] public float FlashlightBattery { get; set; } = 100f;

		private Flashlight _worldFlashlight;
		private Flashlight _viewFlashlight;

		public bool HasFlashlightEntity
		{
			get
			{
				if ( IsLocalPlayer )
				{
					return (_viewFlashlight != null && _viewFlashlight.IsValid());
				}

				return (_worldFlashlight != null && _worldFlashlight.IsValid());
			}
		}

		public bool IsFlashlightOn
		{
			get
			{
				if ( IsLocalPlayer )
					return (HasFlashlightEntity && _viewFlashlight.Enabled);

				return (HasFlashlightEntity && _worldFlashlight.Enabled);
			}
		}

		public void ToggleFlashlight()
		{
			if ( IsLocalPlayer )
			{
				ShowFlashlightLocal( !IsFlashlightOn );
				return;
			}

			ShowFlashlight( !IsFlashlightOn );
		}

		public void ShowFlashlight( bool shouldShow, bool playSounds = true )
		{
			if ( HasFlashlightEntity )
			{
				_worldFlashlight.Enabled = false;
				_worldFlashlight.Flicker = false;
			}

			if ( ActiveChild is not Weapon weapon || !weapon.HasFlashlight )
				return;

			if ( shouldShow )
			{
				if ( !HasFlashlightEntity )
				{
					_worldFlashlight = new Flashlight();
					_worldFlashlight.EnableHideInFirstPerson = true;
					_worldFlashlight.Rot = EyeRot;
					_worldFlashlight.SetParent( weapon, "muzzle" );
					_worldFlashlight.Pos = Vector3.Zero;
				}
				else
				{
					// TODO: This is a weird hack to make sure the rotation is right.
					_worldFlashlight.SetParent( null );
					_worldFlashlight.Rot = EyeRot;
					_worldFlashlight.SetParent( weapon, "muzzle" );
					_worldFlashlight.Pos = Vector3.Zero;
					_worldFlashlight.Enabled = true;
				}

				_worldFlashlight.FogStength = 10f;
				_worldFlashlight.UpdateFromBattery( FlashlightBattery );
				_worldFlashlight.Reset();

				if ( playSounds )
					PlaySound( "flashlight-on" );
			}
			else if ( playSounds )
			{
				PlaySound( "flashlight-off" );
			}

			ShowFlashlightLocal( this, shouldShow );
		}

		[ClientRpc]
		private void ShowFlashlightLocal( bool shouldShow )
		{
			if ( shouldShow )
			{
				if ( !HasFlashlightEntity )
				{
					_viewFlashlight = new Flashlight();
					_viewFlashlight.Rot = EyeRot;
					_viewFlashlight.Pos = EyePos + EyeRot.Forward * 10f;
				}

				_viewFlashlight.FogStength = 10f;
				_viewFlashlight.Enabled = true;
				_viewFlashlight.UpdateFromBattery( FlashlightBattery );
				_viewFlashlight.Reset();
			}
			else if ( HasFlashlightEntity )
			{
				_viewFlashlight.Enabled = false;
			}
		}

		private void TickFlashlight()
		{
			if ( IsFlashlightOn )
			{
				FlashlightBattery = MathF.Max( FlashlightBattery - 10f * Time.Delta, 0f );

				using ( Prediction.Off() )
				{
					if ( IsServer )
					{
						var shouldTurnOff = _worldFlashlight.UpdateFromBattery( FlashlightBattery );

						if ( shouldTurnOff )
							ShowFlashlight( false, false );
					}
					else
					{
						var viewFlashlightParent = _viewFlashlight.Parent;

						if ( ActiveChild is Weapon weapon && weapon.ViewModelEntity != null )
						{
							if ( viewFlashlightParent != weapon.ViewModelEntity )
							{
								_viewFlashlight.SetParent( null );
								_viewFlashlight.Rot = EyeRot;
								_viewFlashlight.SetParent( weapon.ViewModelEntity, "muzzle" );
								_viewFlashlight.Pos = Vector3.Zero;
							}
						}
						else
						{
							if ( viewFlashlightParent != null )
								_viewFlashlight.SetParent( null );

							_viewFlashlight.Rot = EyeRot;
							_viewFlashlight.Pos = EyePos + EyeRot.Forward * 80f;
						}

						_viewFlashlight.UpdateFromBattery( FlashlightBattery );
						_viewFlashlight.BrightnessMultiplier = IsFirstPersonMode ? 1f : 0f;
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
