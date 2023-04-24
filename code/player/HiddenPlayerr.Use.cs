using Sandbox;
using System;

namespace Facepunch.Hidden
{
	public partial class HiddenPlayer
	{
		public Entity Using { get; protected set; }

		protected virtual void TickPlayerUse()
		{
			if ( !Game.IsServer ) return;

			using ( Prediction.Off() )
			{
				if ( Input.Pressed( "use" ) )
				{
					Using = FindUsable();

					if ( Using == null )
					{
						UseFail();
						return;
					}
				}

				if ( !Input.Down( "use" ) )
				{
					StopUsing();
					return;
				}

				if ( !Using.IsValid() )
					return;

				if ( Using is IUse use && use.OnUse( this ) )
					return;

				StopUsing();
			}
		}

		protected virtual void UseFail()
		{
			//PlaySound( "player_use_fail" );
		}

		protected virtual void StopUsing()
		{
			Using = null;
		}

		protected bool IsValidUseEntity( Entity entity )
		{
			if ( entity == null ) return false;
			if ( entity is not IUse use ) return false;
			if ( !use.IsUsable( this ) ) return false;

			return true;
		}

		protected virtual Entity FindUsable()
		{
			var trace = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 85 )
				.Ignore( this )
				.Run();

			var entity = trace.Entity;
			while ( entity.IsValid() && !IsValidUseEntity( entity ) )
			{
				entity = entity.Parent;
			}

			if ( !IsValidUseEntity( entity ) )
			{
				trace = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 85 )
				.Radius( 2 )
				.Ignore( this )
				.Run();

				entity = trace.Entity;
				while ( entity.IsValid() && !IsValidUseEntity( entity ) )
				{
					entity = entity.Parent;
				}
			}

			if ( !IsValidUseEntity( entity ) )
				return null;

			return entity;
		}
	}
}
