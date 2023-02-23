using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Hidden;

public static partial class Util
{
	public static partial class Weapons
	{
		[ClientRpc]
		public static void PlayFlybySound( string sound )
		{
			Sound.FromScreen( sound );
		}

		public static IEnumerable<T> GetBlastEntities<T>( Vector3 position, float radius ) where T : Entity
		{
			var proximity = Entity.FindInSphere( position, radius )
				.OfType<T>()
				.Where( v =>
				{
					var trace = Trace.Ray( position, v.WorldSpaceBounds.Center )
					 .WorldAndEntities()
					 .Ignore( v )
					 .WithTag( "blastproof" )
					 .Run();

					return trace.Fraction >= 0.95f;
				} );

			return proximity;
		}

		public static IEnumerable<Entity> GetBlastEntities( Vector3 position, float radius )
		{
			var proximity = Entity.FindInSphere( position, radius )
				.Where( v =>
				{
					var trace = Trace.Ray( position, v.WorldSpaceBounds.Center )
				 .WorldAndEntities()
				 .Ignore( v )
				 .WithTag( "blastproof" )
				 .Run();

					return trace.Fraction >= 0.95f;
				} );

			return proximity;
		}

		public static float GetDamageFalloff( float distance, float damage, float start, float end )
		{
			if ( end > 0f )
			{
				if ( start > 0f )
				{
					if ( distance < start )
					{
						return damage;
					}

					var falloffRange = end - start;
					var difference = (distance - start);

					return Math.Max( damage - (damage / falloffRange) * difference, 0f );
				}

				return Math.Max( damage - (damage / end) * distance, 0f );
			}

			return damage;
		}

		public static void PlayFlybySounds( Entity attacker, Entity victim, Vector3 start, Vector3 end, float minDistance, float maxDistance, List<string> sounds )
		{
			var sound = Game.Random.FromList( sounds );

			foreach ( var client in Game.Clients )
			{
				var pawn = client.Pawn as Entity;

				if ( !pawn.IsValid() || pawn == attacker )
					continue;

				if ( pawn.LifeState != LifeState.Alive )
					continue;

				var distance = pawn.Position.DistanceToLine( start, end, out var _ );

				if ( distance >= minDistance && distance <= maxDistance )
				{
					PlayFlybySound( To.Single( client ), sound );
				}
			}
		}

		public static void PlayFlybySounds( Entity attacker, Vector3 start, Vector3 end, float minDistance, float maxDistance, List<string> sounds )
		{
			PlayFlybySounds( attacker, null, start, end, minDistance, maxDistance, sounds );
		}
	}
}
