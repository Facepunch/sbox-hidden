using Sandbox;
using System.Threading.Tasks;

namespace Facepunch.Hidden
{
	[Library]
	public class PipeBombConfig : WeaponConfig
	{
		public override string Name => "Pipe Bomb";
		public override string ClassName => "hdn_pipe_bomb";
		public override string Icon => "ui/weapons/pipebomb.png";
		public override AmmoType AmmoType => AmmoType.Grenade;
		public override WeaponType Type => WeaponType.Projectile;
		public override int Ammo => 3;
		public override int Damage => 60;
	}

	[Library( "hdn_pipe_bomb" )]
	public partial class PipeBomb : Throwable<BouncingProjectile>
	{
		public override WeaponConfig Config => new PipeBombConfig();
		//public override string TrailEffect => "particles/weapons/fireball/fireball_trail.vpcf";
		public override string ProjectileModel => "models/grenade/w_grenade.vmdl";
		public override string ThrowSound => "pipebomb.throw";
		public override DamageFlags DamageType => DamageFlags.Blast;
		public override float ProjectileStartRange => 100f;
		public override float ProjectileLifeTime => 3f;
		public override string HitSound => null;
		public override bool HasLaserDot => false;
		public override bool HasFlashlight => false;
		public override float Gravity => 15f;
		public override float Speed => 800f;

		public virtual float BlastRadius => 256f;

		protected override Vector3 AdjustProjectileVelocity( Vector3 velocity )
		{
			return velocity + Vector3.Up * 200f;
		}

		protected override void OnCreateProjectile( BouncingProjectile projectile )
		{
			projectile.BounceSoundMinimumVelocity = 40f;
			projectile.Bounciness = 0.7f;
			projectile.BounceSound = "pipebomb.bounce";
			projectile.FaceDirection = false;
			projectile.FromWeapon = this;
			projectile.Scale = 0.5f;

			Sound.FromEntity( "pipebomb.tickloop", projectile );

			base.OnCreateProjectile( projectile );
		}

		protected override async void OnProjectileHit( BouncingProjectile projectile, TraceResult trace )
		{
			try
			{
				ModelEntity placeholder = null;

				if ( IsServer )
				{
					placeholder = new ModelEntity( ProjectileModel );
					placeholder.Transform = projectile.Transform;
				}

				var position = projectile.Position - projectile.Velocity.Normal * projectile.Radius;

				Sound.FromWorld( "pipebomb.tick", position );

				await GameTask.DelaySeconds( 1f );

				var explosion = Particles.Create( "particles/explosion/barrel_explosion/explosion_barrel.vpcf" );
				explosion.SetPosition( 0, position );

				Sound.FromWorld( "rust_pumpshotgun.shootdouble", position );

				placeholder?.Delete();

				if ( IsClient ) return;

				DamageInRadius( position, BlastRadius, Config.Damage, 10f );
			}
			catch ( TaskCanceledException )
			{

			}
		}
	}
}
