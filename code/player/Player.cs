using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	public partial class Player : Sandbox.Player
	{
		[Net, Predicted] public TimeUntil StaminaRegenTime { get; set; }
		[Net, Predicted] public float Stamina { get; set; }
		[Net, Local] public SenseAbility Sense { get; set; }
		[Net, Local] public ScreamAbility Scream { get; set; }
		[Net, Local] public DeploymentType Deployment { get; set; }
		[Net] public ModelEntity PickupEntity { get; set; }

		public RealTimeSince TimeSinceLastHit { get; private set; }
		public ProjectileSimulator Projectiles { get; private set; }
		public bool IsSenseActive { get; set; }

		private Rotation LastCameraRotation = Rotation.Identity;
		private TimeSince TimeSinceLastFootstep;
		private DamageInfo LastDamageInfo;
		private PhysicsBody PickupEntityBody;
		private Particles SenseParticles;
		private Particles StealthParticles;
		private TimeUntil NextLonelyCheck;
		private bool IsLonely;
		private Sound? TiredSoundLoop;
		private float TiredSoundVolume;
		private Sound? HeartbeatLoop;
		private float HeartbeatVolume;
		private float WalkBob = 0;
		private float Lean = 0;
		private float FOV = 0;

		[ConCmd.Server]
		public static void PlayVoiceCmd( string name )
		{
			if ( ConsoleSystem.Caller is Player player )
			{
				player.PlaySound( name );
			}
		}

		public bool HasTeam
		{
			get => Team != null;
		}

		public Player()
		{
			Projectiles = new( this );
			Inventory = new Inventory( this );
			Animator = new StandardPlayerAnimator();
			Ammo = new List<int>();
		}

		public bool IsSpectator
		{
			get => (CameraMode is SpectateCamera);
		}

		public void MakeSpectator( Vector3 position = default )
		{
			EnableAllCollisions = false;
			EnableDrawing = false;
			Controller = null;
			CameraMode = new SpectateCamera
			{
				DeathPosition = position,
				TimeSinceDied = 0,
				IsHidden = Team is HiddenTeam
			};
		}

		public virtual void RenderHud( Vector2 screenSize )
		{
			if ( ActiveChild is Weapon weapon && weapon.IsValid() )
			{
				weapon.RenderHud( screenSize );
			}
		}

		[ClientRpc]
		public virtual void OnClientKilled()
		{
			TiredSoundLoop?.Stop();
			TiredSoundLoop = null;

			HeartbeatLoop?.Stop();
			HeartbeatLoop = null;

			IsLonely = false;
		}

		public override void Respawn()
		{
			Game.Instance?.Round?.OnPlayerSpawn( this );

			RemoveRagdollEntity();
			DrawPlayer( true );

			PickupEntityBody = null;
			PickupEntity = null;

			Stamina = 100f;

			base.Respawn();
		}

		public override void OnKilled()
		{
			base.OnKilled();

			ShowFlashlight( false, false );
			ShowSenseParticles( false );
			DrawPlayer( false );

			BecomeRagdollOnServer( LastDamageInfo.Force, GetHitboxBone( LastDamageInfo.HitboxIndex ) );

			Inventory.DeleteContents();

			Team?.OnPlayerKilled( this );

			OnClientKilled();
		}

		public override void Simulate( Client client )
		{
			Projectiles.Simulate();

			SimulateActiveChild( client, ActiveChild );
			TickFlashlight();

			if ( Input.ActiveChild != null )
			{
				ActiveChild = Input.ActiveChild;
			}

			if ( LifeState != LifeState.Alive )
			{
				if ( IsServer )
					DestroyLaserDot();

				return;
			}

			TickPlayerUse();

			if ( IsServer )
			{
				using ( Prediction.Off() )
				{
					TickPickupRagdollOrProp();
					SimulateLaserDot( client );
				}
			}

			if ( Team != null )
			{
				Team.Simulate( this );
			}

			if ( ActiveChild is Weapon weapon && !weapon.IsUsable() && weapon.TimeSincePrimaryAttack > 0.5f && weapon.TimeSinceSecondaryAttack > 0.5f )
			{
				SwitchToBestWeapon();
			}

			var controller = GetActiveController();
			controller?.Simulate( client, this, GetActiveAnimator() );
		}

		protected override void UseFail()
		{
			// Do nothing. By default this plays a sound that we don't want.
		}

		public void DrawPlayer( bool shouldDraw )
		{
			EnableDrawing = shouldDraw;
			Clothing.ForEach( x => x.EnableDrawing = shouldDraw );
		}

		[ClientRpc]
		public void ShowHitMarker( int hitboxGroup )
		{
			if ( hitboxGroup == 1 )
				Sound.FromScreen( "hitmarker.headshot" );
			else
				Sound.FromScreen( "hitmarker.hit" );

			TimeSinceLastHit = 0f;
		}

		[ClientRpc]
		public void ShowSenseParticles( bool shouldShow )
		{
			if ( SenseParticles != null )
			{
				SenseParticles.Destroy();
				SenseParticles = null;
			}

			if ( shouldShow )
			{
				SenseParticles = Particles.Create( "particles/sense.vpcf" );

				if ( SenseParticles != null )
					SenseParticles.SetEntity( 0, this, true );
			}
		}

		public void SwitchToBestWeapon()
		{
			var best = Children.Select( x => x as Weapon )
				.Where( x => x.IsValid() && x.IsUsable() )
				.FirstOrDefault();

			if ( best == null ) return;

			ActiveChild = best;
		}

		public override void OnActiveChildChanged( Entity from, Entity to )
		{
			if ( to is Weapon && HasFlashlightEntity )
			{
				ShowFlashlight( false );
			}

			base.OnActiveChildChanged( from, to );
		}

		public override void OnAnimEventFootstep( Vector3 position, int foot, float volume )
		{
			if ( LifeState == LifeState.Dead || !IsClient )
				return;

			if ( TimeSinceLastFootstep < 0.2f )
				return;

			volume *= FootstepVolume();

			TimeSinceLastFootstep = 0f;

			var trace = Trace.Ray( position, position + Vector3.Down * 20f )
				.Radius( 1f )
				.Ignore( this )
				.Run();

			if ( !trace.Hit ) return;

			trace.Surface.DoFootstep( this, trace, foot, volume );
		}

		public override float FootstepVolume()
		{
			var scale = Team is HiddenTeam ? 0.5f : 1f;
			return Velocity.WithZ( 0f ).Length.LerpInverse( 0f, 300f ) * scale;
		}

		public override void PostCameraSetup( ref CameraSetup setup )
		{
			base.PostCameraSetup( ref setup );

			if ( LastCameraRotation == Rotation.Identity )
				LastCameraRotation = CurrentView.Rotation;

			var angleDiff = Rotation.Difference( LastCameraRotation, CurrentView.Rotation );
			var angleDiffDegrees = angleDiff.Angle();
			var allowance = 20.0f;

			if ( angleDiffDegrees > allowance )
			{
				LastCameraRotation = Rotation.Lerp( LastCameraRotation, CurrentView.Rotation, 1.0f - (allowance / angleDiffDegrees) );
			}

			if ( CameraMode is FirstPersonCamera camera )
			{
				AddCameraEffects( camera );
			}
		}

		private void TickPickupRagdollOrProp()
		{
			if ( PickupEntity.IsValid() && PickupEntity.Position.Distance( Position ) > 300f )
			{
				PickupEntityBody = null;
				PickupEntity = null;
			}

			var trace = Trace.Ray( Input.Position, Input.Position + Input.Rotation.Forward * 100f )
				.EntitiesOnly()
				.WithoutTags( "stuck" )
				.Ignore( ActiveChild )
				.Ignore( this )
				.Radius( 2f )
				.Run();

			if ( PickupEntityBody.IsValid() )
			{
				var velocity = PickupEntityBody.Velocity;
				Vector3.SmoothDamp( PickupEntityBody.Position, Input.Position + Input.Rotation.Forward * 100f, ref velocity, 0.2f, Time.Delta * 2f );
				PickupEntityBody.AngularVelocity = Vector3.Zero;
				PickupEntityBody.Velocity = velocity.ClampLength( 400f );
			}

			if ( !Input.Pressed( InputButton.Use ) )
				return;

			var entity = trace.Entity;

			if ( trace.Hit && entity is ModelEntity model && model.PhysicsEnabled )
			{
				if ( !PickupEntityBody.IsValid() && model.CollisionBounds.Size.Length < 128f )
				{
					if ( trace.Body.Mass < 100f )
					{
						PickupEntityBody = trace.Body;
						PickupEntity = model;
						PickupEntity.Tags.Add( "held" );
						return;
					}
				}
			}

			if ( PickupEntityBody.IsValid() )
			{
				trace = Trace.Ray( Input.Position, Input.Position + Input.Rotation.Forward * 80f )
					.WorldOnly()
					.Ignore( ActiveChild )
					.Ignore( this )
					.Radius( 2f )
					.Run();

				if ( PickupEntityBody.IsValid() )
				{
					if ( PickupEntity.IsValid() )
					{
						if ( PickupEntity is PlayerCorpse && trace.Hit )
						{
							PickupEntityBody.Position = trace.EndPosition + trace.Direction * -8f;
							PickupEntity.Tags.Add( "stuck" );

							PhysicsJoint.CreateLength( PhysicsPoint.World( Map.Physics.Body, trace.EndPosition ), PickupEntityBody, 8f );
						}
						else
						{
							PickupEntityBody.ApplyImpulse( Input.Rotation.Forward * 500f * PickupEntityBody.Mass );
						}

						PickupEntity.Tags.Remove( "held" );
					}
				}

				PickupEntityBody = null;
			}
		}

		private void AddCameraEffects( CameraMode camera )
		{
			var speed = Velocity.Length.LerpInverse( 0, 320 );
			var forwardspeed = Velocity.Normal.Dot( camera.Rotation.Forward );

			var left = camera.Rotation.Left;
			var up = camera.Rotation.Up;

			if ( GroundEntity != null )
			{
				WalkBob += Time.Delta * 25.0f * speed;
			}

			camera.Position += up * MathF.Sin( WalkBob ) * speed * 2;
			camera.Position += left * MathF.Sin( WalkBob * 0.6f ) * speed * 1;

			Lean = Lean.LerpTo( Velocity.Dot( camera.Rotation.Right ) * 0.01f, Time.Delta * 15.0f );

			var appliedLean = Lean;
			appliedLean += MathF.Sin( WalkBob ) * speed * 0.3f;
			camera.Rotation *= Rotation.From( 0, 0, appliedLean );

			speed = (speed - 0.7f).Clamp( 0, 1 ) * 3.0f;

			FOV = FOV.LerpTo( speed * 20 * MathF.Abs( forwardspeed ), Time.Delta * 4.0f );

			camera.FieldOfView += FOV;
		}

		public override void TakeDamage( DamageInfo info )
		{
			if ( !Game.Instance.Round.CanPlayerTakeDamage )
			{
				return;
			}

			if ( info.HitboxIndex == 0 )
			{
				info.Damage *= 2.0f;
			}

			if ( info.Attacker is Player attacker && attacker != this )
			{
				if ( !Game.FriendlyFire && attacker.Team == Team )
				{
					return;
				}

				Team?.OnTakeDamageFromPlayer( this, attacker, info );
				attacker.Team?.OnDealDamageToPlayer( attacker, this, info );

				var hitboxGroup = GetHitboxGroup( info.HitboxIndex );
				attacker.ShowHitMarker( To.Single( attacker ), hitboxGroup );
			}

			TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position, info.Flags );

			if ( info.Flags.HasFlag( DamageFlags.Fall ) )
			{
				PlaySound( "fall" );
			}
			else if ( info.Flags.HasFlag( DamageFlags.Bullet ) )
			{
				if ( !Team?.PlayPainSounds( this ) == false )
				{
					PlaySound( "grunt" + Rand.Int( 1, 4 ) );
				}
			}

			LastDamageInfo = info;

			base.TakeDamage( info );
		}

		public void RemoveRagdollEntity()
		{
			if ( Ragdoll != null && Ragdoll.IsValid() )
			{
				Ragdoll.Delete();
				Ragdoll = null;
			}
		}

		[ClientRpc]
		public void TookDamage( Vector3 position, DamageFlags flags )
		{
			if ( flags.HasFlag( DamageFlags.Fall ) )
				return;

			DamageIndicator.Current?.OnHit( position );
		}

		[Event.Frame]
		protected virtual void OnFrame()
		{
			if ( ActiveChild is Weapon weapon && LaserDot.IsValid() && LifeState == LifeState.Alive )
			{
				var attachment = weapon.EffectEntity.GetAttachment( "laser" );
				if ( !attachment.HasValue ) return;

				var position = EyePosition;
				var rotation = EyeRotation;

				if ( !LaserDot.IsAuthority )
				{
					position = attachment.Value.Position;
					rotation = attachment.Value.Rotation;
				}

				var trace = Trace.Ray( position, position + rotation.Forward * 4096f )
					.UseHitboxes()
					.Radius( 2f )
					.Ignore( weapon )
					.Ignore( this )
					.Run();

				var start = attachment.Value.Position;
				var end = trace.EndPosition;

				LaserDot.LaserParticles.SetPosition( 0, start );
				LaserDot.LaserParticles.SetPosition( 1, end );

				if ( LaserDot.IsAuthority )
				{
					LaserDot.Position = end;
				}
			}
		}

		[Event.Tick.Client]
		protected virtual void ClientTick()
		{
			if ( Team is HiddenTeam && !IsLocalPawn && LifeState == LifeState.Alive )
			{
				if ( StealthParticles == null )
				{
					StealthParticles = Particles.Create( "particles/hidden_effect.vpcf", this );
					StealthParticles.SetEntity( 0, this );
				}

				StealthParticles.SetPosition( 1, Velocity.Length.Remap( 0f, 400f, 0.3f, 1f ).Clamp( 0.3f, 1f ) * 0.5f );
			}
			else
			{
				StealthParticles?.Destroy( true );
			}

			if ( SenseParticles != null )
			{
				var color = Color.Lerp( Color.Red, Color.Green, (1f / 100f) * Health );
				SenseParticles.SetPosition( 1, color * 255f );
			}

			if ( Stamina < 30 )
			{
				if ( !TiredSoundLoop.HasValue )
				{
					TiredSoundLoop = Sound.FromEntity( Team is HiddenTeam ? "sprint.tired.hidden" : "sprint.tired", this );
				}

				TiredSoundVolume = TiredSoundVolume.LerpTo( 1f, Time.Delta * 3f );
				TiredSoundLoop.Value.SetVolume( TiredSoundVolume );
			}
			else if ( TiredSoundLoop.HasValue )
			{
				TiredSoundVolume = TiredSoundVolume.LerpTo( 0f, Time.Delta * 2f );
				TiredSoundLoop.Value.SetVolume( TiredSoundVolume );

				if ( TiredSoundVolume.AlmostEqual( 0f ) )
				{
					TiredSoundLoop.Value.Stop();
					TiredSoundLoop = null;
				}
			}

			if ( Team is IrisTeam && NextLonelyCheck )
			{
				var otherPlayersNearby = FindInSphere( Position, 4096f )
					.OfType<Player>()
					.Where( p => p.Team is IrisTeam && p != this )
					.Count();

				NextLonelyCheck = 1f;
				IsLonely = otherPlayersNearby == 0;
			}
			else
			{
				IsLonely = false;
			}

			if ( IsLonely )
			{
				if ( !HeartbeatLoop.HasValue )
				{
					HeartbeatLoop = Sound.FromEntity( "heartbeat.loop", this );
				}

				HeartbeatVolume = HeartbeatVolume.LerpTo( 1f, Time.Delta * 3f );
				HeartbeatLoop.Value.SetVolume( HeartbeatVolume );
			}
			else if ( HeartbeatLoop.HasValue )
			{
				HeartbeatVolume = HeartbeatVolume.LerpTo( 0f, Time.Delta * 2f );
				HeartbeatLoop.Value.SetVolume( HeartbeatVolume );

				if ( HeartbeatVolume.AlmostEqual( 0f ) )
				{
					HeartbeatLoop.Value.Stop();
					HeartbeatLoop = null;
				}
			}
		}

		protected override void OnDestroy()
		{
			ShowSenseParticles( false );
			RemoveRagdollEntity();

			StealthParticles?.Destroy( true );
			TiredSoundLoop?.Stop();
			HeartbeatLoop?.Stop();

			if ( IsServer )
			{
				DestroyLaserDot();
			}

			base.OnDestroy();
		}
	}
}
