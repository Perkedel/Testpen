using Sandbox.MovementPlus;
namespace Sandbox;

[Icon( "directions_walk" )]
[EditorHandle( Icon = "directions_walk" )]
[Title( "Player Controller Plus" )]
[Category( "Physics" )]
[HelpUrl( "https://sbox.game/dev/doc/scene/components/reference/player-controller/" )]
public sealed partial class PlayerControllerPlus : Component, IScenePhysicsEvents, Component.ExecuteInEditor
{
	/// <summary>
	/// This is used to keep a distance away from surfaces. For example, when grounding, we'll
	/// be a skin distance away from the ground.
	/// </summary>
	const float _skin = 0.095f;

	public CameraComponent CameraOverride { get; set; }
	public int InputPlayerIndex { get; set; } = -1;

	[Property, Hide, RequireComponent] public Rigidbody Body { get; set; }

	public CapsuleCollider BodyCollider { get; private set; }
	public BoxCollider FeetCollider { get; private set; }

	[Property, Hide]
	public GameObject ColliderObject { get; private set; }

	bool _showRigidBodyComponent;


	[Property, Group( "Body" ), Range( 1, 64 )] public float BodyRadius { get; set; } = 16.0f;
	[Property, Group( "Body" ), Range( 1, 128 )] public float BodyHeight { get; set; } = 72.0f;
	[Property, Group( "Body" ), Range( 1, 1000 )] public float BodyMass { get; set; } = 500;
	[Property, Group( "Body" )] public TagSet BodyCollisionTags { get; set; }

	/// <summary>
	/// We will apply extra friction when we're on the ground and our desired velocity is
	/// lower than our current velocity, so we will slow down.
	/// </summary>
	[Property, Group( "Physics" ), Range( 0, 1 )] public float BrakePower { get; set; } = 1;

	/// <summary>
	/// How much friction to add when we're in the air. This will slow you down unless you have a wish
	/// velocity.
	/// </summary>
	[Property, Group( "Physics" ), Range( 0, 1 )] public float AirFriction { get; set; } = 0.1f;


	[Property, Group( "Components" ), Title( "Show Rigidbody" )]
	public bool ShowRigidbodyComponent
	{
		get => _showRigidBodyComponent;
		set
		{
			_showRigidBodyComponent = value;

			if ( Body.IsValid() )
			{
				Body.Flags = Body.Flags.WithFlag( ComponentFlags.Hidden, !value );
			}
		}
	}

	bool _showColliderComponent;

	[Property, Group( "Components" ), Title( "Show Colliders" )]
	public bool ShowColliderComponents
	{
		get => _showColliderComponent;
		set
		{
			_showColliderComponent = value;

			if ( ColliderObject.IsValid() )
			{
				ColliderObject.Flags = ColliderObject.Flags.WithFlag( GameObjectFlags.Hidden, !value );
			}

			if ( BodyCollider.IsValid() )
			{
				BodyCollider.Flags = BodyCollider.Flags.WithFlag( ComponentFlags.Hidden, !value );
			}

			if ( FeetCollider.IsValid() )
			{
				FeetCollider.Flags = FeetCollider.Flags.WithFlag( ComponentFlags.Hidden, !value );
			}
		}
	}

	[FeatureEnabled( "Extended", Icon = "tune" )]
	public bool UseExtendedFeatures { get; set; } = true;

	[Property, Feature( "Extended" ), Title( "Enable Extended Features" )] public bool ExtendedFeaturesEnabled { get; set; }

	/// <summary>
	/// How much the character resists changes in movement direction. 0 = instant direction changes, 1 = very sluggish.
	/// </summary>
	[Property, Feature( "Extended" ), Group( "General" ), Range( 0, 1 )] public float Inertia { get; set; } = 0;

	/// <summary>
	/// When enabled, horizontal velocity is preserved when landing instead of being braked immediately.
	/// </summary>
	[Property, Feature( "Extended" ), Group( "General" )] public bool PreserveVelocityOnLanding { get; set; }

	/// <summary>
	/// Show a debug overlay with velocity, move state, and grounded state.
	/// </summary>
	[Property, Feature( "Extended" ), Group( "General" )] public bool ShowStats { get; set; }

	/// <summary>
	/// In first person, render the body normally but make head/hair/hats shadow-only so they don't clip into the camera.
	/// Requires HideBodyInFirstPerson to be false.
	/// </summary>
	[Property, Feature( "Extended" ), Group( "Camera" )] public bool TrueFirstPerson { get; set; }

	/// <summary>
	/// Offset from the head bone position for the first-person camera.
	/// </summary>
	[Property, Feature( "Extended" ), Group( "Camera" )] public Vector3 TrueFirstPersonOffset { get; set; } = new Vector3( 0, 0, 0 );

	/// <summary>
	/// Completely decouples the camera from the player. The camera stays in place
	/// and the body only rotates toward its movement direction.
	/// </summary>
	[Property, Feature( "Extended" ), Group( "Camera" )] public bool DisableCameraBinding { get; set; }

	[Sync]
	public Vector3 WishVelocity { get; set; }

	public bool IsOnGround => GroundObject.IsValid();

	/// <summary>
	/// Not touching the ground, and not swimming or climbing
	/// </summary>
	public bool IsAirborne => !IsOnGround && !IsSwimming && !IsClimbing;

	/// <summary>
	/// Our actual physical velocity minus our ground velocity
	/// </summary>
	public Vector3 Velocity { get; private set; }

	/// <summary>
	/// The velocity that the ground underneath us is moving
	/// </summary>
	public Vector3 GroundVelocity { get; set; }

	/// <summary>
	/// Set to true when entering a climbing <see cref="MoveModePlus"/>.
	/// </summary>
	public bool IsClimbing { get; set; }

	/// <summary>
	/// Set to true when entering a swimming <see cref="MoveModePlus"/>.
	/// </summary>
	public bool IsSwimming { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		// Some scenes/prefabs may have saved the old colliders before
		// we moved them to their own GameObject. If that's the case then
		// we should destroy them right now to avoid any trouble.
		{
			var bc = GetComponent<BoxCollider>();
			var cc = GetComponent<CapsuleCollider>();

			if ( bc.IsValid() && cc.IsValid() && !bc.IsTrigger && !cc.IsTrigger )
			{
				bc.Destroy();
				cc.Destroy();
			}
		}

		CreateModeInstances();
		Mode = _walkMode;

		EnsureComponentsCreated();
		UpdateBody();

		Body.Velocity = 0;
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		EnableAnimationEvents();

		if ( !Scene.IsEditor )
		{
			EyeAngles = WorldRotation.Angles() with { pitch = 0, roll = 0 };
			WorldRotation = Rotation.Identity;

			if ( Renderer is not null ) Renderer.WorldRotation = new Angles( 0, EyeAngles.yaw, 0 );
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		CleanupTrueFirstPerson();

		ColliderObject?.Destroy();
		ColliderObject = default;

		Body?.Destroy();
		Body = default;
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		DisableAnimationEvents();
		StopPressing();
	}

	protected override void OnValidate()
	{
		EnsureComponentsCreated();
		UpdateBody();
	}

	void IScenePhysicsEvents.PrePhysicsStep()
	{
		UpdateBody();

		if ( IsProxy )
			return;

		using var scope = InputPlayerIndex >= 0 ? Input.PlayerScope( InputPlayerIndex ) : null;

		Mode.AddVelocity();
		Mode.PrePhysicsStep();
	}

	void IScenePhysicsEvents.PostPhysicsStep()
	{
		Velocity = Body.Velocity - GroundVelocity;
		UpdateGroundVelocity();

		RestoreStep();

		Mode?.PostPhysicsStep();
		CategorizeGround();

		ChooseBestMoveMode();
	}

	Transform _groundTransform;

	void UpdateGroundVelocity()
	{
		if ( GroundObject is null )
		{
			GroundVelocity = 0;
			return;
		}

		if ( GroundComponent is Collider collider )
		{
			GroundVelocity = collider.GetVelocityAtPoint( WorldPosition );
		}

		if ( GroundComponent is Rigidbody rigidbody )
		{
			var mass1 = BodyMass;
			var mass2 = rigidbody.Mass;
			var massFactor = mass2 / (mass1 + mass2);
			GroundVelocity = rigidbody.GetVelocityAtPoint( WorldPosition ) * massFactor;
		}
	}

	/// <summary>
	/// Adds velocity in a special way. First we subtract any opposite velocity (ie, falling) then 
	/// we add the velocity, but we clamp it to that direction. This means that if you jump when you're running
	/// up a platform, you don't get extra jump power.
	/// </summary>
	public void Jump( Vector3 velocity )
	{
		PreventGrounding( 0.2f );

		var currentVel = Body.Velocity;

		// moving in the opposite direction
		// because this is a jump, we want to counteract that
		var dot = currentVel.Dot( velocity );
		if ( dot < 0 )
		{
			currentVel = currentVel.SubtractDirection( velocity.Normal, 1 );
		}

		currentVel = currentVel.AddClamped( velocity, velocity.Length );

		Body.Velocity = currentVel;
	}

	void UpdateEyeTransform()
	{
		EyeTransform = Mode?.CalculateEyeTransform() ?? WorldTransform;
	}

}
