using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Hidden
{
	public partial class SpectateCamera : CameraMode
	{
		[Net] public TimeSince TimeSinceDied { get; set; }
		[Net] public Vector3 DeathPosition { get; set; }
		[Net] public bool IsHidden { get; set; }

		public Player TargetPlayer { get; set; }
		public CCTVCamera CCTVEntity { get; set; }

		private List<CCTVCamera> CCTVEntities;
		private Sound StaticLoopSound;
		private Vector3 FocusPoint;
		private int TargetIdx;

		public override void Activated()
		{
			base.Activated();

			FocusPoint = CurrentView.Position - GetViewOffset();
			FieldOfView = 70f;
			CCTVEntities = Entity.All.OfType<CCTVCamera>().ToList();

			StaticLoopSound = Sound.FromScreen( "cctv.static" );

			InputHints.UpdateOnClient();
		}

		public override void Deactivated()
		{
			StaticLoopSound.Stop();

			base.Deactivated();
		}

		public override void Update()
		{
			if ( Local.Pawn is not Player player )
				return;

			if ( !IsHidden )
			{
				if ( TargetPlayer == null || !TargetPlayer.IsValid() || Input.Pressed( InputButton.PrimaryAttack ) )
				{
					var players = Game.Instance.GetTeamPlayers<IrisTeam>( true );

					if ( players != null && players.Count > 0 )
					{
						if ( ++TargetIdx >= players.Count )
							TargetIdx = 0;

						TargetPlayer = players[TargetIdx];
					}
				}

				if ( CCTVEntity == null || !CCTVEntity.IsValid() || Input.Pressed( InputButton.PrimaryAttack ) )
				{
					var cameras = CCTVEntities;

					if ( cameras.Count > 0 )
					{
						if ( ++TargetIdx >= cameras.Count )
							TargetIdx = 0;

						CCTVEntity = cameras[TargetIdx];
					}
				}
			}

			FocusPoint = Vector3.Lerp( FocusPoint, GetSpectatePoint(), Time.Delta * 5f );

			if ( !IsHidden && CCTVEntity.IsValid() )
			{
				Position = CCTVEntity.Position;
				Rotation = CCTVEntity.Rotation;
			}
			else
			{
				Position = FocusPoint + GetViewOffset();
				Rotation = player.EyeRotation;
			}

			FieldOfView = FieldOfView.LerpTo( 50, Time.Delta * 3f );
			Viewer = null;
		}

		private Vector3 GetSpectatePoint()
		{
			if ( Local.Pawn is not Player )
				return DeathPosition;

			if ( TargetPlayer == null || !TargetPlayer.IsValid() || TimeSinceDied < 3 )
				return DeathPosition;

			return TargetPlayer.EyePosition;
		}

		private Vector3 GetViewOffset()
		{
			if ( Local.Pawn is not Player player )
				return Vector3.Zero;

			return player.EyeRotation.Forward * -150f + Vector3.Up * 10f;
		}
	}
}
