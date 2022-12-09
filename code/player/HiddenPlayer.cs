using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.Physics;
using Sandbox.Component;

namespace Facepunch.Hidden
{
	public partial class HiddenPlayer : AnimatedEntity
	{
		public static HiddenPlayer Me => Local.Pawn as HiddenPlayer;

		private static List<Particles> AllBloodParticles { get; set; } = new();

		public static void ClearAllBloodParticles()
		{
			foreach ( var particles in AllBloodParticles )
			{
				particles?.Destroy();
			}

			AllBloodParticles.Clear();
		}

		public static void AddBloodParticle( Particles particles )
		{
			AllBloodParticles.Add( particles );
		}

		[Net, Predicted] public TimeUntil StaminaRegenTime { get; set; }
		[Net, Predicted] public TimeSince TimeSinceLastLeap { get; set; }
		[Net, Predicted] public float Stamina { get; set; }
		[Net, Predicted] public bool IsFrozen { get; set; }
		[Net] public SenseAbility Sense { get; set; }
		[Net] public ScreamAbility Scream { get; set; }
		[Net] public DeploymentType Deployment { get; set; }
		[Net] public TimeSince TimeSinceDroppedEntity { get; set; }
		[Net] public Vector3 DeathPosition { get; set; }
		[Net] public TimeSince TimeSinceDied { get; set; }
		[Net] public ModelEntity PickupEntity { get; set; }
		[Net] public int UniqueRandomSeed { get; set; }
		[Net] public Color RandomColor { get; set; }

		[Net, Predicted] public MoveController Controller { get; private set; }
		[Net, Predicted] public Entity ActiveChild { get; set; }
		[ClientInput] public Vector3 InputDirection { get; protected set; }
		[ClientInput] public Entity ActiveChildInput { get; set; }
		[ClientInput] public Angles ViewAngles { get; set; }
		public Angles OriginalViewAngles { get; private set; }

		public RealTimeSince TimeSinceLastHit { get; private set; }
		public ProjectileSimulator Projectiles { get; private set; }
		public Inventory Inventory { get; private set; }
		public ICamera CurrentCamera { get; private set; }

		public Vector3 EyePosition
		{
			get => Transform.PointToWorld( EyeLocalPosition );
			set => EyeLocalPosition = Transform.PointToLocal( value );
		}

		[Net, Predicted]
		public Vector3 EyeLocalPosition { get; set; }

		public Rotation EyeRotation
		{
			get => Transform.RotationToWorld( EyeLocalRotation );
			set => EyeLocalRotation = Transform.RotationToLocal( value );
		}

		[Net, Predicted]
		public Rotation EyeLocalRotation { get; set; }

		public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

		private class LegsClothingObject
		{
			public SceneModel SceneObject { get; set; }
			public Clothing Asset { get; set; }
		}

		private Entity LastActiveChild { get; set; }
		private Particles HealthBloodDrip { get; set; }
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
			if ( ConsoleSystem.Caller.Pawn is HiddenPlayer player )
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

		public HiddenPlayer()
		{
			Projectiles = new( this );
			Inventory = new Inventory( this );
			Ammo = new List<int>();
		}

		public float WaterLevel
		{
			get
			{
				var c = Components.Get<WaterEffectComponent>();
				return c?.WaterLevel ?? 0;
			}
		}

		public bool IsSpectator
		{
			get => CurrentCamera is SpectateCamera;
		}

		public void SetMoveController<T>() where T : MoveController, new()
		{
			Controller = new T();
		}

		public void PlayRadioCommand( RadioCommandResource resource )
		{
			Assert.NotNull( resource );

			var radioColor = Color.Green.Lighten( 0.5f ).Desaturate( 0.5f );
			var irisPlayers = Game.Instance.GetTeamPlayers<IrisTeam>();

			ChatBox.AddChatFromServer( To.Multiple( irisPlayers.Select( p => p.Client ) ), this, $"*beep* {resource.Text}", radioColor, radioColor );

			var playersCloseBy = All.OfType<HiddenPlayer>().Where( p => p != this && p.Position.Distance( Position ) <= resource.ProximityDistance );
			var inRadioRange = irisPlayers.Except( playersCloseBy );

			Sound.FromScreen( To.Multiple( inRadioRange.Select( p => p.Client ) ), resource.Sound.ResourceName );

			if ( resource.ProximitySound == null ) return;
			Sound.FromWorld( To.Multiple( playersCloseBy.Select( p => p.Client ) ), resource.ProximitySound.ResourceName, Position );
		}

		public void MakeSpectator( Vector3 position = default )
		{
			EnableAllCollisions = false;
			EnableDrawing = false;
			DeathPosition = position;
			TimeSinceDied = 0f;
			LifeState = LifeState.Dead;
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
			HealthBloodDrip?.Destroy( true );
			HealthBloodDrip = null;

			KillAllSoundLoops();
			IsLonely = false;
		}

		[ClientRpc]
		public virtual void ClientRespawn()
		{
			TimeSinceLastAlone = 0f;

			HealthBloodDrip?.Destroy( true );
			HealthBloodDrip = null;

			KillAllSoundLoops();
			IsLonely = false;
		}

		public virtual void Respawn()
		{
			Game.Instance?.Round?.OnPlayerSpawn( this );

			RemoveRagdollEntity();
			DrawPlayer( true );

			RandomColor = Color.Random.Lighten( 1f ).Saturate( 2f );

			PickupEntityBody = null;
			PickupEntity = null;
			LifeState = LifeState.Alive;
			Health = 100f;
			Velocity = Vector3.Zero;
			Stamina = 100f;

			InputHints.UpdateOnClient( To.Single( this ) );

			ClientRespawn();
			CreateHull();

			GameManager.Current?.MoveToSpawnpoint( this );
			ResetInterpolation();
		}

		public override void Spawn()
		{
			EnableLagCompensation = true;
			Tags.Add( "player" );

			base.Spawn();
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

				foreach ( var clothing in LegsClothing )
				{
					clothing.SceneObject.Delete();
				}

				LegsClothing.Clear();

				foreach ( var child in Children )
				{
					AddClothingToLegs( child );
				}
			}

			base.OnNewModel( model );
		}

		public override void OnKilled()
		{
			GameManager.Current?.OnKilled( this );

			LifeState = LifeState.Dead;
			StopUsing();

			Client?.AddInt( "deaths", 1 );

			ShowFlashlight( false, false );
			ShowSenseParticles( false );
			DrawPlayer( false );

			if ( LastDamageInfo.HasTag( "blast" ) || LastDamageInfo.Damage >= 100f )
			{
				var gib = Particles.Create( "particles/blood/gib.vpcf", this );
				gib.SetPosition( 0, WorldSpaceBounds.Center );

				var trace = Trace.Ray( WorldSpaceBounds.Center, WorldSpaceBounds.Center + Vector3.Down * 300f )
					.WorldOnly()
					.Run();

				if ( trace.Hit )
				{
					var pool = Particles.Create( "particles/blood/blood_puddle.vpcf", this );
					pool.SetPosition( 0, trace.EndPosition );
					AddBloodParticle( pool );
				}

				CreateBloodExplosion( 6, 600f );
			}
			else
			{
				BecomeRagdollOnServer( LastDamageInfo );
				CreateBloodExplosion( 3, 300f );
			}

			PickupEntityBody = null;
			PickupEntity = null;

			Inventory.DeleteContents();

			Team?.OnPlayerKilled( this );

			OnClientKilled();
		}

		public override void BuildInput()
		{
			OriginalViewAngles = ViewAngles;
			InputDirection = Input.AnalogMove;

			if ( Input.StopProcessing )
				return;

			var look = Input.AnalogLook;

			if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
			{
				look = look.WithYaw( look.yaw * -1f );
			}

			var viewAngles = ViewAngles;
			viewAngles += look;
			viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
			viewAngles.roll = 0f;
			ViewAngles = viewAngles.Normal;

			ActiveChild?.BuildInput();
		}

		public override void Simulate( Client client )
		{
			Projectiles.Simulate();

			SimulateAnimation();
			SimulateActiveChild( ActiveChild );
			TickFlashlight();

			if ( ActiveChildInput.IsValid() && ActiveChildInput.Owner == this )
				ActiveChild = ActiveChildInput;

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

			Controller?.SetActivePlayer( this );
			Controller?.Simulate();
		}

		public void CreateBloodExplosion( int decalCount, float maxDistance = 800f )
		{
			var explosion = Particles.Create( "particles/blood/explosion_blood/explosion_blood.vpcf", this );
			explosion.SetPosition( 0, WorldSpaceBounds.Center );

			var decal = ResourceLibrary.Get<DecalDefinition>( "decals/blood_splatter.decal" );

			for ( var i = 0; i < decalCount; i++ )
			{
				var trace = Trace.Ray( LastDamageInfo.Position, LastDamageInfo.Position + Vector3.Random * maxDistance )
					.Ignore( this )
					.Ignore( ActiveChild )
					.Run();

				if ( trace.Hit )
				{
					Decal.Place( To.Everyone, decal, null, 0, trace.EndPosition - trace.Direction * 1f, Rotation.LookAt( trace.Normal ), Color.White );
				}
			}
		}

		public void CreateBloodShotDecal( DamageInfo info, float maxDistance )
		{
			var decal = ResourceLibrary.Get<DecalDefinition>( "decals/blood_splatter.decal" );

			var trace = Trace.Ray( info.Position, info.Position + info.Force * maxDistance )
				.Ignore( this )
				.Ignore( ActiveChild )
				.Run();

			if ( trace.Hit )
			{
				Decal.Place( To.Everyone, decal, null, 0, trace.EndPosition - trace.Direction * 1f, Rotation.LookAt( trace.Normal ), Color.White );
			}
		}

		public void DrawPlayer( bool shouldDraw )
		{
			EnableDrawing = shouldDraw;
			Clothing.ForEach( x => x.EnableDrawing = shouldDraw );
		}

		[ClientRpc]
		public void ShowHitMarker( bool isHeadshot )
		{
			if ( isHeadshot )
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

		private void AddClothingToLegs( Entity child )
		{
			if ( !AnimatedLegs.IsValid() || child is not ModelEntity model || child is Weapon )
				return;

			if ( model.Model == null ) return;

			var assets = ResourceLibrary.GetAll<Clothing>();
			var asset = assets.FirstOrDefault( a => !string.IsNullOrEmpty( a.Model ) && a.Model.ToLower() == model.Model.Name.ToLower() );
			if ( asset == null ) return;

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

		protected virtual float GetFootstepVolume()
		{
			var scale = Team is HiddenTeam ? 0.5f : 1f;
			return Velocity.WithZ( 0f ).Length.LerpInverse( 0f, 300f ) * scale;
		}

		protected virtual void CreateHull()
		{
			SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16f, -16f, 0f ), new Vector3( 16f, 16f, 72f ) );
			EnableHitboxes = true;
		}

		protected virtual void SimulateActiveChild( Entity child )
		{
			if ( Prediction.FirstTime )
			{
				if ( LastActiveChild != child )
				{
					OnActiveChildChanged( LastActiveChild, child );
					LastActiveChild = child;
				}
			}

			if ( !LastActiveChild.IsValid() )
				return;

			if ( LastActiveChild.IsAuthority )
			{
				LastActiveChild.Simulate( Client );
			}
		}

		protected virtual void OnActiveChildChanged( Entity previous, Entity next )
		{
			if ( previous is Weapon previousWeapon )
			{
				previousWeapon?.ActiveEnd( this, previousWeapon.Owner != this );

				if ( LaserDot.IsValid() )
				{
					DestroyLaserDot();
				}
			}

			if ( next is Weapon nextWeapon )
			{
				nextWeapon?.ActiveStart( this );

				if ( HasFlashlightEntity )
				{
					ShowFlashlight( false );
				}
			}
		}

		private void TickPickupRagdollOrProp()
		{
			if ( PickupEntity.IsValid() && PickupEntity.Position.Distance( Position ) > 300f )
			{
				PickupEntityBody = null;
				PickupEntity = null;
			}

			var trace = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 100f )
				.EntitiesOnly()
				.WithoutTags( "stuck" )
				.Ignore( ActiveChild )
				.Ignore( this )
				.Radius( 2f )
				.Run();

			if ( PickupEntityBody.IsValid() )
			{
				var velocity = PickupEntityBody.Velocity;
				Vector3.SmoothDamp( PickupEntityBody.Position, EyePosition + EyeRotation.Forward * 100f, ref velocity, 0.2f, Time.Delta * 2f );
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
				trace = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 80f )
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

							trace = Trace.Ray( PickupEntityBody.Position, PickupEntityBody.Position + Vector3.Down * 600f )
								.WorldOnly()
								.Run();

							if ( trace.Hit )
							{
								/*
								var pool = Particles.Create( "particles/blood/blood_puddle.vpcf" );
								pool.SetPosition( 0, trace.EndPosition );
								pool.SetForward( 0, Vector3.Up );
								AddBloodParticle( pool );
								*/
							}

							var drip = Particles.Create( "particles/blood/blood_drip.vpcf", PickupEntity );
							drip.SetEntity( 0, PickupEntity );
							drip.SetPosition( 2, new Vector3( 60f ) );
							AddBloodParticle( drip );
						}
						else
						{
							PickupEntityBody.ApplyImpulse( EyeRotation.Forward * 500f * PickupEntityBody.Mass );
						}

						PickupEntity.Tags.Remove( "held" );
					}
				}

				TimeSinceDroppedEntity = 0f;
				PickupEntityBody = null;
				PickupEntity = null;
			}
		}

		private void AddCameraEffects()
		{
			var speed = Velocity.Length.LerpInverse( 0f, 320f );
			var forwardspeed = Velocity.Normal.Dot( Camera.Rotation.Forward );

			var left = Camera.Rotation.Left;
			var up = Camera.Rotation.Up;

			if ( GroundEntity != null )
			{
				WalkBob += Time.Delta * 25f * speed;
			}

			Camera.Position += up * MathF.Sin( WalkBob ) * speed * 2f;
			Camera.Position += left * MathF.Sin( WalkBob * 0.6f ) * speed * 1f;

			Lean = Lean.LerpTo( Velocity.Dot( Camera.Rotation.Right ) * 0.01f, Time.Delta * 15f );

			var appliedLean = Lean;
			appliedLean += MathF.Sin( WalkBob ) * speed * 0.3f;
			Camera.Rotation *= Rotation.From( 0, 0, appliedLean );

			speed = (speed - 0.7f).Clamp( 0f, 1f ) * 3f;

			FOV = FOV.LerpTo( speed * 20f * MathF.Abs( forwardspeed ), Time.Delta * 4f );

			Camera.FieldOfView += FOV;
		}

		public override void OnAnimEventFootstep( Vector3 position, int foot, float volume )
		{
			if ( LifeState == LifeState.Dead || !IsClient )
				return;

			if ( TimeSinceLastFootstep < 0.2f )
				return;

			volume *= GetFootstepVolume();

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

		public override void TakeDamage( DamageInfo info )
		{
			if ( !Game.Instance.Round.CanPlayerTakeDamage )
			{
				return;
			}

			if ( info.Hitbox.HasTag( "head" ) )
			{
				info.Damage *= 2f;
			}

			if ( Team is HiddenTeam )
			{
				info.Damage *= Game.ScaleHiddenDamage;
			}

			if ( info.Attacker is HiddenPlayer attacker && attacker != this )
			{
				if ( !Game.FriendlyFire && attacker.Team == Team )
				{
					return;
				}

				Team?.OnTakeDamageFromPlayer( this, attacker, info );
				attacker.Team?.OnDealDamageToPlayer( attacker, this, info );

				attacker.ShowHitMarker( To.Single( attacker ), info.Hitbox.HasTag( "head" ) );
			}

			if ( info.HasTag( "bullet" ) )
				CreateBloodShotDecal( info, 1000f );
			else if ( info.HasTag( "blunt" ) )
				CreateBloodShotDecal( info, 200f );

			TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position, info.HasTag( "fall" ) );

			if ( info.HasTag( "fall" ) )
			{
				PlaySound( "fall" );
			}
			else if ( info.HasTag( "bullet" ) || info.HasTag( "blunt" ) )
			{
				if ( !Team?.PlayPainSounds( this ) == false )
				{
					PlaySound( "grunt" + Rand.Int( 1, 4 ) );
				}
			}

			LastDamageInfo = info;

			if ( LifeState == LifeState.Alive )
			{
				base.TakeDamage( info );

				this.ProceduralHitReaction( info );

				if ( LifeState == LifeState.Dead && info.Attacker.IsValid() )
				{
					if ( info.Attacker.Client.IsValid() && info.Attacker.IsValid() )
					{
						info.Attacker.Client.AddInt( "kills" );
					}
				}
			}
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
		public void TookDamage( Vector3 position, bool isFallDamage )
		{
			if ( isFallDamage )
				return;

			DamageIndicator.Current?.OnHit( position );
		}

		public override void FrameSimulate( Client cl )
		{
			if ( LifeState == LifeState.Alive )
			{
				if ( CurrentCamera is not FirstPersonCamera )
				{
					CurrentCamera?.Deactivated();
					CurrentCamera = new FirstPersonCamera();
					CurrentCamera.Activated();
				}

				if ( LastCameraRotation == Rotation.Identity )
					LastCameraRotation = Camera.Rotation;

				var angleDiff = Rotation.Difference( LastCameraRotation, Camera.Rotation );
				var angleDiffDegrees = angleDiff.Angle();
				var allowance = 20.0f;

				if ( angleDiffDegrees > allowance )
				{
					LastCameraRotation = Rotation.Lerp( LastCameraRotation, Camera.Rotation, 1.0f - (allowance / angleDiffDegrees) );
				}

				AddCameraEffects();

				Controller?.SetActivePlayer( this );
				Controller?.FrameSimulate();
			}
			else
			{
				if ( CurrentCamera is not SpectateCamera )
				{
					CurrentCamera?.Deactivated();
					CurrentCamera = new SpectateCamera();
					CurrentCamera.Activated();
				}

				var spectateCamera = CurrentCamera as SpectateCamera;
				spectateCamera.DeathPosition = DeathPosition;
				spectateCamera.TimeSinceDied = TimeSinceDied;
				spectateCamera.IsHidden = Team is HiddenTeam;
			}

			CurrentCamera?.Update();
		}

		public override void OnChildAdded( Entity child )
		{
			Inventory?.OnChildAdded( child );
			AddClothingToLegs( child );
		}

		public override void OnChildRemoved( Entity child )
		{
			Inventory?.OnChildRemoved( child );

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

		[Event.Client.Frame]
		protected virtual void OnFrame()
		{
			var weapon = ActiveChild as Weapon;

			if ( IsFlashlightOn && weapon.IsValid() )
			{
				if ( FlashEffect == null )
				{
					FlashEffect = Particles.Create( "particles/flashlight/flashlight.vpcf", weapon.EffectEntity, "laser" );
				}

				FlashEffect.SetEntityAttachment( 0, weapon.EffectEntity, "laser" );
				FlashEffect.SetPosition( 2, new Color( 0.9f, 0.87f, 0.6f ) );
				FlashEffect.SetPosition( 3, new Vector3( 1f, 1f, 0f ) );
			}
			else
			{
				FlashEffect?.Destroy();
				FlashEffect = null;
			}

			if ( weapon.IsValid() && LaserDot.IsValid() && LifeState == LifeState.Alive )
			{
				var attachment = weapon.EffectEntity.GetAttachment( "laser" );
				if ( !attachment.HasValue ) return;

				var position = Camera.Position;
				var rotation = Camera.Rotation;
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
				AnimatedLegs.RenderingEnabled = (Team is IrisTeam && LifeState == LifeState.Alive);

				foreach ( var clothing in LegsClothing )
				{
					clothing.SceneObject.RenderingEnabled = AnimatedLegs.RenderingEnabled;
				}

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

			if ( Team is IrisTeam && LifeState == LifeState.Alive && Health <= 90f )
			{
				if ( HealthBloodDrip == null )
				{
					HealthBloodDrip = Particles.Create( "particles/blood/blood_drip.vpcf", this );
					HealthBloodDrip.SetEntity( 0, this );
				}

				HealthBloodDrip.SetPosition( 2, new Vector3( 100f - Health ) );
			}
			else
			{
				HealthBloodDrip?.Destroy( true );
				HealthBloodDrip = null;
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

			if ( IsLocalPawn )
			{
				DoLonelyLogic();
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

		protected virtual void DoLonelyLogic()
		{
			if ( LifeState == LifeState.Alive )
			{
				if ( NextLonelyCheck )
				{
					var otherPlayersNearby = FindInSphere( Position, 1500f )
						.OfType<HiddenPlayer>()
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
			HealthBloodDrip?.Destroy( true );

			KillAllSoundLoops();
			DestroyLaserDot();

			FlashEffect?.Destroy( true );

			base.OnDestroy();
		}
	}
}
