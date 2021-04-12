using Sandbox.UI.Construct;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace HiddenGamemode
{
	public class Nameplates : Panel
	{
		private readonly Dictionary<Player, Nameplate> _activeNameplates = new();

		public float MaxDrawDistance = 400;
		public int MaxNameplates = 10;

		public Nameplates()
		{
			StyleSheet.Load( "/ui/Nameplates.scss" );
		}

		public override void Tick()
		{
			base.Tick();

			var deleteList = new List<Player>();
			var count = 0;

			deleteList.AddRange( _activeNameplates.Keys );

			foreach ( var v in Sandbox.Player.All.OrderBy( x => Vector3.DistanceBetween( x.EyePos, Camera.LastPos ) ) )
			{
				if ( v is not Player player ) continue;

				if ( UpdateNameplate( player ) )
				{
					deleteList.Remove( player );
					count++;
				}

				if ( count >= MaxNameplates )
					break;
			}

			foreach ( var player in deleteList )
			{
				_activeNameplates[player].Delete();
				_activeNameplates.Remove( player );
			}
		}

		public Nameplate CreateNameplate( Player player )
		{
			var tag = new Nameplate( player )
			{
				Parent = this
			};

			return tag;
		}

		public bool UpdateNameplate( Player player )
		{
			if ( player.IsLocalPlayer || !player.HasTeam || player.Team.HideNameplate )
				return false;

			if ( player.LifeState != LifeState.Alive )
				return false;

			var head = player.GetAttachment( "hat" );

			if ( head.Pos == Vector3.Zero )
				head.Pos = player.EyePos;

			var labelPos = head.Pos + head.Rot.Up * 5;

			float dist = labelPos.Distance( Camera.LastPos );

			if ( dist > MaxDrawDistance )
				return false;

			var localPlayer = Sandbox.Player.Local as Player;

			// If we're not spectating only show nameplates of players we can see.
			if ( !localPlayer.IsSpectator )
			{
				var lookDir = (labelPos - Camera.LastPos).Normal;

				if ( Camera.LastRot.Forward.Dot( lookDir ) < 0.5 )
					return false;

				var trace = Trace.Ray( localPlayer.EyePos, head.Pos )
					.Ignore( localPlayer )
					.Run();

				if ( trace.Entity != player )
					return false;
			}

			var alpha = dist.LerpInverse( MaxDrawDistance, MaxDrawDistance * 0.1f, true );
			var objectSize = 0.05f / dist / (2.0f * MathF.Tan( (Camera.LastFieldOfView / 2.0f).DegreeToRadian() )) * 1500.0f;

			objectSize = objectSize.Clamp( 0.05f, 1.0f );

			if ( !_activeNameplates.TryGetValue( player, out var tag ) )
			{
				tag = CreateNameplate( player );
				_activeNameplates[player] = tag;
			}

			var screenPos = labelPos.ToScreen();

			tag.Style.Left = Length.Fraction( screenPos.x );
			tag.Style.Top = Length.Fraction( screenPos.y );
			tag.Style.Opacity = alpha;

			var transform = new PanelTransform();
			transform.AddTranslateY( Length.Fraction( -1.0f ) );
			transform.AddScale( objectSize );
			transform.AddTranslateX( Length.Fraction( -0.5f ) );

			tag.Style.Transform = transform;
			tag.Style.Dirty();

			return true;
		}
	}
}
