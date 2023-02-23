using Sandbox;
using System;

namespace Facepunch.Hidden;

public abstract partial class ProjectileWeapon<T> : Weapon where T : Projectile, new()
{
	public virtual float ProjectileStartRange => 100f;
	public virtual string ProjectileData => "";
	public virtual float InheritVelocity => 0f;
	public virtual float Spread => 0.05f;

	public override void AttackPrimary()
	{
		if ( Prediction.FirstTime )
            {
			Game.SetRandomSeed( Time.Tick );
			FireProjectile();
            }
	}

	public virtual void FireProjectile()
	{
		if ( Owner is not HiddenPlayer player )
			return;

		var projectile = Projectile.Create<T>( ProjectileData );

		projectile.IgnoreEntity = this;
		projectile.FlybySounds = FlybySounds;
		projectile.Simulator = player.Projectiles;
		projectile.Attacker = player;

		OnCreateProjectile( projectile );

		var forward = player.EyeRotation.Forward;
		var position = player.EyePosition + forward * ProjectileStartRange;
		var muzzle = GetMuzzlePosition();

		if ( muzzle.HasValue )
		{
			position = muzzle.Value;
		}

		var trace = Trace.Ray( player.EyePosition, position )
			.Ignore( player )
			.Ignore( this )
			.Run();

		if ( trace.Hit )
		{
			position = trace.EndPosition - trace.Direction * 16f;
		}

		var endPosition = player.EyePosition + forward * BulletRange;

		trace = Trace.Ray( player.EyePosition, endPosition )
			.Ignore( player )
			.Ignore( this )
			.Run();

		var direction = (trace.EndPosition - position).Normal;
		direction += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * Spread * 0.25f;
		direction = direction.Normal;

		Game.SetRandomSeed( Time.Tick );

		var speed = projectile.Data.Speed.GetValue();
		var velocity = (direction * speed) + (player.Velocity * InheritVelocity);
		velocity = AdjustProjectileVelocity( velocity );
		projectile.Initialize( position, velocity, (a, b) => OnProjectileHit( (T)a, b ) );

		OnProjectileFired( projectile );
	}

	protected virtual Vector3 AdjustProjectileVelocity( Vector3 velocity )
	{
		return velocity;
	}

	protected virtual Vector3? GetMuzzlePosition()
	{
		var muzzle = GetAttachment( MuzzleAttachment );
		return muzzle.HasValue ? muzzle.Value.Position : null;
	}

	protected virtual void OnProjectileFired( T projectile )
	{

	}

	protected virtual float ModifyDamage( Entity victim, float damage )
	{
		return damage;
	}

	protected virtual void DamageInRadius( Vector3 position, float radius, float baseDamage, float force = 1f )
	{
		var entities = Util.Weapons.GetBlastEntities( position, radius );

		foreach ( var entity in entities )
		{
			var direction = (entity.Position - position).Normal;
			var distance = entity.Position.Distance( position );
			var damage = Math.Max( baseDamage - ((baseDamage / radius) * distance), 0f );

			damage = ModifyDamage( entity, damage );

			DealDamage( entity, position, direction * 100f * force, damage );
		}
	}

	protected virtual void OnCreateProjectile( T projectile )
	{

	}

	protected virtual void OnProjectileHit( T projectile, TraceResult trace )
	{
		if ( Game.IsServer && trace.Entity.IsValid() )
		{
			var distance = trace.EndPosition.Distance( projectile.StartPosition );
			var damage = GetDamageFalloff( distance, Config.Damage );
			DealDamage( trace.Entity, projectile.Position, projectile.Velocity * 0.1f, damage );
		}
	}
}
