using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public class IrisController : CustomWalkController
	{
		public float FallDamageVelocity = 550f;
		public float FallDamageScale = 0.25f;
		public float MaxSprintSpeed = 300f;

		private float _fallVelocity;

		public override void Tick()
		{
			if ( Player is Player player )
			{
				if ( Input.Down( InputButton.Run ) && Velocity.Length >= (SprintSpeed * 0.8f) )
				{
					player.Stamina = MathF.Max( player.Stamina - (15f * Time.Delta), 0f );
				}
				else
				{
					player.Stamina = MathF.Min( player.Stamina + (10f * Time.Delta), 100f );
				}

				SprintSpeed = WalkSpeed + (((MaxSprintSpeed - WalkSpeed) / 100f) * player.Stamina) + 40f;
			}

			base.Tick();
		}

		protected override void OnPreTickMove()
		{
			_fallVelocity = Velocity.z;
		}

		protected override void OnPostCategorizePosition( bool stayOnGround, TraceResult trace )
		{
			if ( Host.IsServer && trace.Hit && _fallVelocity < -FallDamageVelocity )
			{
				var damage = (MathF.Abs( _fallVelocity ) - FallDamageVelocity) * FallDamageScale;

				using ( Prediction.Off() )
				{
					var damageInfo = new DamageInfo()
						.WithAttacker( Player )
						.WithFlag( DamageFlags.Fall );

					damageInfo.Damage = damage;

					Player.TakeDamage( damageInfo );
				}
			}
		}
	}
}
