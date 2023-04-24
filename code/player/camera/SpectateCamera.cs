using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Hidden
{
	public class SpectateCamera : ICamera
	{
		public TimeSince TimeSinceDied { get; set; }
		public Vector3 DeathPosition { get; set; }
		public bool IsHidden { get; set; }

		public HiddenPlayer TargetPlayer { get; set; }
		public CCTVCamera CCTVEntity { get; set; }

		private List<CCTVCamera> CCTVEntities;
		private Sound StaticLoopSound;
		private Vector3 FocusPoint;
		private int TargetIdx;

		public void Activated()
		{
			StaticLoopSound = Sound.FromScreen( "cctv.static" );
			CCTVEntities = Entity.All.OfType<CCTVCamera>().ToList();
			FocusPoint = Camera.Position - GetViewOffset();
			InputHints.UpdateOnClient();
		}

		public void Deactivated()
		{
			StaticLoopSound.Stop();
		}

		public void Update()
		{
			if ( Game.LocalPawn is not HiddenPlayer player )
				return;

			if ( !IsHidden )
			{
				if ( TargetPlayer == null || !TargetPlayer.IsValid() || Input.Pressed( "attack1" ) )
				{
					var players = HiddenGame.Entity.GetTeamPlayers<IrisTeam>( true );
					var count = players.Count();

					if ( players != null && count > 0 )
					{
						if ( ++TargetIdx >= count )
							TargetIdx = 0;

						TargetPlayer = players.ElementAt( TargetIdx );
					}
				}

				if ( CCTVEntity == null || !CCTVEntity.IsValid() || Input.Pressed( "attack1" ) )
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
				Camera.Position = CCTVEntity.Position;
				Camera.Rotation = CCTVEntity.Rotation;
			}
			else
			{
				Camera.Position = FocusPoint + GetViewOffset();
				Camera.Rotation = player.EyeRotation;
			}

			Camera.FieldOfView = Camera.FieldOfView.LerpTo( 50f, Time.Delta * 3f );
			Camera.FirstPersonViewer = null;

			ScreenShake.Apply();
		}

		private Vector3 GetSpectatePoint()
		{
			if ( Game.LocalPawn is not HiddenPlayer )
				return DeathPosition;

			if ( TargetPlayer == null || !TargetPlayer.IsValid() || TimeSinceDied < 3 )
				return DeathPosition;

			return TargetPlayer.EyePosition;
		}

		private Vector3 GetViewOffset()
		{
			if ( Game.LocalPawn is not HiddenPlayer player )
				return Vector3.Zero;

			return player.EyeRotation.Forward * -150f + Vector3.Up * 10f;
		}
	}
}
