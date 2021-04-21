using Sandbox;
using System;

namespace HiddenGamemode
{
	partial class Player
	{
		[NetLocalPredicted] public float FlashlightBattery { get; set; } = 100f;

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
			ShowFlashlight( !IsFlashlightOn );
		}

		public void ShowFlashlight( bool shouldShow, bool playSounds = true )
		{
			if ( IsFlashlightOn )
			{
				if ( IsServer )
					_worldFlashlight.Enabled = false;
				else
					_viewFlashlight.Enabled = false;
			}

			if ( IsServer && IsFlashlightOn != shouldShow )
				ShowFlashlightLocal( this, shouldShow );

			if ( ActiveChild is not Weapon weapon || !weapon.HasFlashlight )
				return;

			if ( shouldShow )
			{
				if ( !HasFlashlightEntity )
				{
					if ( IsServer )
					{
						_worldFlashlight = new Flashlight();
						_worldFlashlight.EnableHideInFirstPerson = true;
						_worldFlashlight.LocalRot = EyeRot;
						_worldFlashlight.SetParent( weapon, "muzzle" );
						_worldFlashlight.LocalPos = Vector3.Zero;
					}
					else
					{
						_viewFlashlight = new Flashlight();
						_viewFlashlight.EnableViewmodelRendering = true;
						_viewFlashlight.WorldRot = EyeRot;
						_viewFlashlight.WorldPos = EyePos + EyeRot.Forward * 10f;
					}
				}
				else
				{
					if ( IsServer )
					{
						// TODO: This is a weird hack to make sure the rotation is right.
						_worldFlashlight.SetParent( null );
						_worldFlashlight.LocalRot = EyeRot;
						_worldFlashlight.SetParent( weapon, "muzzle" );
						_worldFlashlight.LocalPos = Vector3.Zero;
						_worldFlashlight.Enabled = true;
					}
					else
					{
						_viewFlashlight.Enabled = true;
					}
				}

				if ( IsServer )
				{
					_worldFlashlight.FogStength = 10f;
					_worldFlashlight.UpdateFromBattery( FlashlightBattery );
					_worldFlashlight.Reset();
				}
				else
				{
					_viewFlashlight.FogStength = 10f;
					_viewFlashlight.UpdateFromBattery( FlashlightBattery );
					_viewFlashlight.Reset();
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
								_viewFlashlight.SetParent( weapon.ViewModelEntity, "muzzle" );
								_viewFlashlight.WorldRot = EyeRot;
								_viewFlashlight.LocalPos = Vector3.Zero;
							}
						}
						else
						{
							if ( viewFlashlightParent != null )
								_viewFlashlight.SetParent( null );

							_viewFlashlight.WorldRot = EyeRot;
							_viewFlashlight.WorldPos = EyePos + EyeRot.Forward * 80f;
						}

						var shouldTurnOff = _viewFlashlight.UpdateFromBattery( FlashlightBattery );

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
