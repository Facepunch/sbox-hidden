using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	public struct AnimationHelperWithLegs
	{
		private HiddenPlayer Owner { get; set; }
		private SceneModel Legs => Owner.AnimatedLegs;

		public AnimationHelperWithLegs( HiddenPlayer entity )
		{
			Owner = entity;
		}

		public void WithLookAt( Vector3 look, float eyesWeight = 1.0f, float headWeight = 1.0f, float bodyWeight = 1.0f )
		{
			var aimRay = Owner.AimRay;

			Owner.SetAnimLookAt( "aim_eyes", aimRay.Position, look );
			Owner.SetAnimLookAt( "aim_head", aimRay.Position, look );
			Owner.SetAnimLookAt( "aim_body", aimRay.Position, look );

			SetAnimParameter( "aim_eyes_weight", eyesWeight );
			SetAnimParameter( "aim_head_weight", headWeight );
			SetAnimParameter( "aim_body_weight", bodyWeight );
		}

		public void WithVelocity( Vector3 Velocity )
		{
			var dir = Velocity;
			var forward = Owner.Rotation.Forward.Dot( dir );
			var sideward = Owner.Rotation.Right.Dot( dir );

			var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

			SetAnimParameter( "move_direction", angle );
			SetAnimParameter( "move_speed", Velocity.Length );
			SetAnimParameter( "move_groundspeed", Velocity.WithZ( 0 ).Length );
			SetAnimParameter( "move_y", sideward );
			SetAnimParameter( "move_x", forward );
			SetAnimParameter( "move_z", Velocity.z );
		}

		public void WithWishVelocity( Vector3 Velocity )
		{
			var dir = Velocity;
			var forward = Owner.Rotation.Forward.Dot( dir );
			var sideward = Owner.Rotation.Right.Dot( dir );

			var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

			SetAnimParameter( "wish_direction", angle );
			SetAnimParameter( "wish_speed", Velocity.Length );
			SetAnimParameter( "wish_groundspeed", Velocity.WithZ( 0 ).Length );
			SetAnimParameter( "wish_y", sideward );
			SetAnimParameter( "wish_x", forward );
			SetAnimParameter( "wish_z", Velocity.z );
		}

		public Rotation AimAngle
		{
			set
			{
				value = Owner.Rotation.Inverse * value;
				var ang = value.Angles();

				SetAnimParameter( "aim_body_pitch", ang.pitch );
				SetAnimParameter( "aim_body_yaw", ang.yaw );
			}
		}

		public float AimEyesWeight
		{
			get => Owner.GetAnimParameterFloat( "aim_eyes_weight" );
			set => SetAnimParameter( "aim_eyes_weight", value );
		}

		public float AimHeadWeight
		{
			get => Owner.GetAnimParameterFloat( "aim_head_weight" );
			set => SetAnimParameter( "aim_head_weight", value );
		}

		public float AimBodyWeight
		{
			get => Owner.GetAnimParameterFloat( "aim_body_weight" );
			set => SetAnimParameter( "aim_headaim_body_weight_weight", value );
		}


		public float FootShuffle
		{
			get => Owner.GetAnimParameterFloat( "move_shuffle" );
			set => SetAnimParameter( "move_shuffle", value );
		}

		public float DuckLevel
		{
			get => Owner.GetAnimParameterFloat( "duck" );
			set => SetAnimParameter( "duck", value );
		}

		public float VoiceLevel
		{
			get => Owner.GetAnimParameterFloat( "voice" );
			set => SetAnimParameter( "voice", value );
		}

		public bool IsSitting
		{
			get => Owner.GetAnimParameterBool( "b_sit" );
			set => SetAnimParameter( "b_sit", value );
		}

		public bool IsGrounded
		{
			get => Owner.GetAnimParameterBool( "b_grounded" );
			set => SetAnimParameter( "b_grounded", value );
		}

		public bool IsSwimming
		{
			get => Owner.GetAnimParameterBool( "b_swim" );
			set => SetAnimParameter( "b_swim", value );
		}

		public bool IsClimbing
		{
			get => Owner.GetAnimParameterBool( "b_climbing" );
			set => SetAnimParameter( "b_climbing", value );
		}

		public bool IsNoclipping
		{
			get => Owner.GetAnimParameterBool( "b_noclip" );
			set => SetAnimParameter( "b_noclip", value );
		}

		public bool IsWeaponLowered
		{
			get => Owner.GetAnimParameterBool( "b_weapon_lower" );
			set => SetAnimParameter( "b_weapon_lower", value );
		}

		public enum HoldTypes
		{
			None,
			Pistol,
			Rifle,
			Shotgun,
			HoldItem,
			Punch,
			Swing
		}

		public HoldTypes HoldType
		{
			get => (HoldTypes)Owner.GetAnimParameterInt( "holdtype" );
			set => Owner.SetAnimParameter( "holdtype", (int)value );
		}

		public enum Hand
		{
			Both,
			Right,
			Left
		}

		public Hand Handedness
		{
			get => (Hand)Owner.GetAnimParameterInt( "holdtype_handedness" );
			set => Owner.SetAnimParameter( "holdtype_handedness", (int)value );
		}

		public void TriggerJump()
		{
			SetAnimParameter( "b_jump", true );
		}

		public void TriggerDeploy()
		{
			SetAnimParameter( "b_deploy", true );
		}

		private HashSet<string> IgnoredLegParams = new HashSet<string>
		{
			"b_shuffle",
			"aim_body"
		};

		public void SetAnimParameter( string name, Vector3 val )
		{
			Owner.SetAnimParameter( name, val );

			if ( !IgnoredLegParams.Contains( name ) )
				Legs?.SetAnimParameter( name, val );
		}

		public void SetAnimParameter( string name, float val )
		{
			Owner.SetAnimParameter( name, val );

			if ( !IgnoredLegParams.Contains( name ) )
				Legs?.SetAnimParameter( name, val );
		}

		public void SetAnimParameter( string name, bool val )
		{
			Owner.SetAnimParameter( name, val );

			if ( !IgnoredLegParams.Contains( name ) )
				Legs?.SetAnimParameter( name, val );
		}

		public void SetAnimParameter( string name, int val )
		{
			Owner.SetAnimParameter( name, val );

			if ( !IgnoredLegParams.Contains( name ) )
				Legs?.SetAnimParameter( name, val );
		}

		public void ResetAnimParameters()
		{
			Legs?.ResetAnimParameters();
			Owner.ResetAnimParameters();
		}
	}
}
