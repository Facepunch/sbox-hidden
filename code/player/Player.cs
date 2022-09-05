using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Facepunch.Hidden
{
	public partial class Player : Sandbox.Player
	{
		[Net, Predicted] public TimeUntil StaminaRegenTime { get; set; }
		[Net, Predicted] public TimeSince TimeSinceLastLeap { get; set; }
		[Net, Predicted] public float Stamina { get; set; }
		[Net] public SenseAbility Sense { get; set; }
		[Net] public ScreamAbility Scream { get; set; }
		[Net] public DeploymentType Deployment { get; set; }
		[Net] public TimeSince TimeSinceDroppedEntity { get; set; }
		[Net] public ModelEntity PickupEntity { get; set; }
		[Net] public int UniqueRandomSeed { get; set; }
		[Net] public Color RandomColor { get; set; }

		public RealTimeSince TimeSinceLastHit { get; private set; }
		public ProjectileSimulator Projectiles { get; private set; }

		private class LegsClothingObject
		{
			public SceneModel SceneObject { get; set; }
			public Clothing Asset { get; set; }
		}

		private List<LegsClothingObject> LegsClothing { get; set; } = new();

		public SceneModel AnimatedLegs { get; private set; }

		public bool IsSenseActive { get; set; }

		private HashSet<string> LegBonesToKeep = new()
		{
			"leg_upper_R_twist",
			"leg_upper_R",
			"leg_upper_L",
			"leg_upper_L_twist",
			"leg_lower_L",
			"leg_lower_R",
			"ankle_L",
			"ankle_R",
			"ball_L",
			"ball_R",
			"leg_knee_helper_L",
			"leg_knee_helper_R",
			"leg_lower_R_twist",
			"leg_lower_L_twist"
		};

		private Rotation LastCameraRotation = Rotation.Identity;
		private TimeSince TimeSinceLastFootstep;
		private DamageInfo LastDamageInfo;
		private PhysicsBody PickupEntityBody;
		private RealTimeUntil NextSearchDeadBodies { get; set; }
		private TimeSince TimeSinceLastAlone { get; set; }
		private Particles SenseParticles;
		private Particles StealthParticles;
		private TimeUntil NextLonelyCheck;
		private bool IsLonely;
		private Sound? BodyDragSound;
		private Sound? TiredSoundLoop;
		private float TiredSoundVolume;
		private Sound? LonelyLoopSound;
		private float LonelySoundVolume;
		private float WalkBob = 0f;
		private float Lean = 0f;
		private float FOV = 0f;

		[ConCmd.Server( "hdn_radio" )]
		public static void PlayVoiceCmd( int resourceId )
		{
			if ( ConsoleSystem.Caller.Pawn is Player player )
			{
				var resource = ResourceLibrary.GetAll<RadioCommandResource>().FirstOrDefault( c => c.ResourceId == resourceId );
				if ( resource == null ) return;

				player.PlayRadioCommand( resource );
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
			Animator = new AnimatorWithLegs();
			//Transmit = TransmitType.Always;
			Ammo = new List<int>();
		}

		public bool IsSpectator
		{
			get => (CameraMode is SpectateCamera);
		}

		public void PlayRadioCommand( RadioCommandResource resource )
		{
			Assert.NotNull( resource );

			var radioColor = Color.Green.Lighten( 0.5f ).Desaturate( 0.5f );

			ChatBox.AddChatFromServer( this, $"*beep* {resource.Text}", radioColor, radioColor );

			var irisPlayers = Game.Instance.GetTeamPlayers<IrisTeam>();
			var playersCloseBy = All.OfType<Player>().Where( p => p != this && p.Position.Distance( Position ) <= resource.ProximityDistance );
			var inRadioRange = irisPlayers.Except( playersCloseBy );

			Sound.FromScreen( To.Multiple( inRadioRange.Select( p => p.Client ) ), resource.Sound.ResourceName );

			if ( resource.ProximitySound == null ) return;
			Sound.FromWorld( To.Multiple( playersCloseBy.Select( p => p.Client ) ), resource.ProximitySound.ResourceName, Position );
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
			KillAllSoundLoops();
			IsLonely = false;
		}

		[ClientRpc]
		public virtual void ClientRespawn()
		{
			KillAllSoundLoops();
			IsLonely = false;
		}

		public override void Respawn()
		{
			Game.Instance?.Round?.OnPlayerSpawn( this );

			RemoveRagdollEntity();
			DrawPlayer( true );

			RandomColor = Color.Random.Lighten( 1f ).Saturate( 2f );

			PickupEntityBody = null;
			PickupEntity = null;

			Stamina = 100f;

			InputHints.UpdateOnClient( To.Single( this ) );

			ClientRespawn();

			base.Respawn();
		}

		public override void OnNewModel( Model model )
		{
			if ( IsLocalPawn )
			{
				if ( AnimatedLegs.IsValid() )
				{
					AnimatedLegs.Delete();
					AnimatedLegs = null;
				}

				AnimatedLegs = new( Map.Scene, model, Transform );
				AnimatedLegs.SetBodyGroup( "Head", 1 );
				AnimatedLegs.SetBodyGroup( "Chest", 1 );
			}

			base.OnNewModel( model );
		}

		public override void OnKilled()
		{
			base.OnKilled();

			ShowFlashlight( false, false );
			ShowSenseParticles( false );
			DrawPlayer( false );

			BecomeRagdollOnServer( LastDamageInfo );

			PickupEntityBody = null;
			PickupEntity = null;

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
					if ( Team is HiddenTeam )
						TickPickupRagdollOrProp();
					else
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
			if ( from is Weapon && LaserDot.IsValid() )
			{
				DestroyLaserDot();
			}

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

			if ( Team is IrisTeam )
			{
				var sound = PlaySound( "add.walking" );
				sound.SetVolume( volume * 0.3f );
			}
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
							PickupEntity.PlaySound( "body.stick" );
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

				TimeSinceDroppedEntity = 0f;
				PickupEntityBody = null;
				PickupEntity = null;
			}
		}

		private void AddCameraEffects( CameraMode camera )
		{
			var speed = Velocity.Length.LerpInverse( 0f, 320f );
			var forwardspeed = Velocity.Normal.Dot( camera.Rotation.Forward );

			var left = camera.Rotation.Left;
			var up = camera.Rotation.Up;

			if ( GroundEntity != null )
			{
				WalkBob += Time.Delta * 25f * speed;
			}

			camera.Position += up * MathF.Sin( WalkBob ) * speed * 2f;
			camera.Position += left * MathF.Sin( WalkBob * 0.6f ) * speed * 1f;

			Lean = Lean.LerpTo( Velocity.Dot( camera.Rotation.Right ) * 0.01f, Time.Delta * 15f );

			var appliedLean = Lean;
			appliedLean += MathF.Sin( WalkBob ) * speed * 0.3f;
			camera.Rotation *= Rotation.From( 0, 0, appliedLean );

			speed = (speed - 0.7f).Clamp( 0f, 1f ) * 3f;

			FOV = FOV.LerpTo( speed * 20f * MathF.Abs( forwardspeed ), Time.Delta * 4f );

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
				info.Damage *= 2f;
			}

			if ( Team is HiddenTeam )
			{
				info.Damage *= Game.ScaleHiddenDamage;
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
			else if ( info.Flags.HasFlag( DamageFlags.Bullet ) || info.Flags.HasFlag( DamageFlags.Blunt ) )
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

		public override void OnChildAdded( Entity child )
		{
			base.OnChildAdded( child );

			if ( AnimatedLegs.IsValid() && child is ModelEntity model && child is not Weapon )
			{
				if ( model.Model == null ) return;

				var assets = ResourceLibrary.GetAll<Clothing>();
				var asset = assets.FirstOrDefault( a => !string.IsNullOrEmpty( a.Model ) && a.Model.ToLower() == model.Model.Name.ToLower() );

				if ( asset != null )
				{
					if ( asset.Category == Sandbox.Clothing.ClothingCategory.Bottoms
						|| asset.Category == Sandbox.Clothing.ClothingCategory.Footwear
						|| asset.Category == Sandbox.Clothing.ClothingCategory.Tops )
					{
						var clothing = new SceneModel( Map.Scene, model.Model, AnimatedLegs.Transform );
						AnimatedLegs.AddChild( "clothing", clothing );

						LegsClothing.Add( new()
						{
							SceneObject = clothing,
							Asset = asset
						} );
					}
				}
			}
		}

		public override void OnChildRemoved( Entity child )
		{
			base.OnChildRemoved( child );

			if ( AnimatedLegs.IsValid() && child is ModelEntity model && child is not Weapon )
			{
				if ( model.Model == null ) return;

				var indexOf = LegsClothing.FindIndex( 0, c => c.Asset.Model.ToLower() == model.Model.Name.ToLower() );

				if ( indexOf >= 0 )
				{
					var clothing = LegsClothing[indexOf];
					LegsClothing.RemoveAt( indexOf );
					clothing.SceneObject.Delete();
				}
			}
		}

		[Event.Tick.Server]
		protected virtual void ServerTick()
		{
			if ( !NextSearchDeadBodies || LifeState == LifeState.Dead || Team is not IrisTeam )
				return;

			var bodiesNearby = FindInSphere( Position, 3000f )
				.OfType<PlayerCorpse>()
				.Where( r => !r.HasBeenFound );

			foreach ( var body in bodiesNearby )
			{
				var trace = Trace.Ray( EyePosition, body.Position )
					.WorldOnly()
					.Run();

				if ( trace.Fraction >= 0.9f )
				{
					using ( Prediction.Off() )
					{
						var resource = RadioCommandResource.FindByName( "body.found" );
						PlayRadioCommand( resource );
						body.HasBeenFound = true;
					}
				}
			}

			NextSearchDeadBodies = 1f;
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

				if ( !LaserDot.IsAuthority || weapon.IsReloading )
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

				LaserDot.LaserParticles.SetPosition( 2, RandomColor * 255f );
				LaserDot.DotParticles.SetPosition( 2, RandomColor * 255f );

				LaserDot.LaserParticles.SetPosition( 0, start );
				LaserDot.LaserParticles.SetPosition( 1, end );

				if ( LaserDot.IsAuthority )
				{
					LaserDot.Position = end;
				}
			}

			if ( AnimatedLegs.IsValid() )
			{
				AnimatedLegs.RenderingEnabled = (Team is IrisTeam && LifeState == LifeState.Alive)

				if ( AnimatedLegs.RenderingEnabled )
				{
					var shouldHideLegs = LegsClothing.Any( c => c.Asset.HideBody.HasFlag( Sandbox.Clothing.BodyGroups.Legs ) );

					AnimatedLegs.SetBodyGroup( "Head", 1 );
					AnimatedLegs.SetBodyGroup( "Hands", 1 );
					AnimatedLegs.SetBodyGroup( "Legs", shouldHideLegs ? 1 : 0 );

					AnimatedLegs.Flags.CastShadows = false;
					AnimatedLegs.Transform = Transform;
					AnimatedLegs.Position += AnimatedLegs.Rotation.Forward * -10f;

					AnimatedLegs.Update( RealTime.Delta );

					foreach ( var clothing in LegsClothing )
					{
						clothing.SceneObject.Flags.CastShadows = false;
						clothing.SceneObject.Update( RealTime.Delta );

						UpdateAnimatedLegBones( clothing.SceneObject );
					}

					UpdateAnimatedLegBones( AnimatedLegs );
				}
			}
		}

		protected void UpdateAnimatedLegBones( SceneModel model )
		{
			for ( var i = 0; i < model.Model.BoneCount; i++ )
			{
				var boneName = model.Model.GetBoneName( i );

				if ( !LegBonesToKeep.Contains( boneName ) )
				{
					var moveBackBy = 25f;

					if ( boneName == "spine_1" ) moveBackBy = 15f;
					if ( boneName == "spine_0" ) moveBackBy = 10f;
					if ( boneName == "pelvis" ) moveBackBy = 5f;

					var transform = model.GetBoneWorldTransform( i );
					transform.Position += model.Rotation.Backward * moveBackBy;
					transform.Position += model.Rotation.Up * 15f;
					model.SetBoneWorldTransform( i, transform );
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

			if ( LifeState == LifeState.Alive )
			{
				if ( NextLonelyCheck )
				{
					var otherPlayersNearby = FindInSphere( Position, 1500f )
						.OfType<Player>()
						.Where( p => p != this );

					if ( Team is IrisTeam )
					{
						otherPlayersNearby = otherPlayersNearby.Where( p => p.Team == Team );
					}

					NextLonelyCheck = 1f;
					IsLonely = !otherPlayersNearby.Any();
				}
			}
			else
			{
				IsLonely = false;
			}

			if ( IsLonely )
			{
				if ( !LonelyLoopSound.HasValue )
				{
					LonelyLoopSound = Sound.FromEntity( Team is HiddenTeam ? "hidden.whispers" : "heartbeat.loop", this );

					if ( TimeSinceLastAlone > 20f && Rand.Float() >= 0.5f )
					{
						var resource = RadioCommandResource.FindByName( "alone" );
						PlayVoiceCmd( resource.ResourceId );
						TimeSinceLastAlone = 0f;
					}
				}

				LonelySoundVolume = LonelySoundVolume.LerpTo( 1f, Time.Delta * 3f );
				LonelyLoopSound.Value.SetVolume( LonelySoundVolume );
			}
			else if ( LonelyLoopSound.HasValue )
			{
				LonelySoundVolume = LonelySoundVolume.LerpTo( 0f, Time.Delta * 2f );
				LonelyLoopSound.Value.SetVolume( LonelySoundVolume );

				if ( LonelySoundVolume.AlmostEqual( 0f ) )
				{
					LonelyLoopSound.Value.Stop();
					LonelyLoopSound = null;
				}
			}

			if ( PickupEntity.IsValid() && PickupEntity is PlayerCorpse )
			{
				if ( !BodyDragSound.HasValue )
				{
					BodyDragSound = Sound.FromEntity( "body.drag", PickupEntity );
				}
			}
			else if ( BodyDragSound.HasValue )
			{
				BodyDragSound.Value.Stop();
				BodyDragSound = null;
			}
		}

		protected virtual void KillAllSoundLoops()
		{
			TiredSoundLoop?.Stop();
			TiredSoundLoop = null;

			LonelyLoopSound?.Stop();
			LonelyLoopSound = null;

			BodyDragSound?.Stop();
			BodyDragSound = null;
		}

		protected override void OnDestroy()
		{
			ShowSenseParticles( false );
			RemoveRagdollEntity();

			StealthParticles?.Destroy( true );

			KillAllSoundLoops();
			DestroyLaserDot();

			FlashEffect?.Destroy( true );

			base.OnDestroy();
		}
	}
}
