namespace Sandbox.MovementPlus;

/// <summary>
/// Base class for external move modes that plug into PlayerControllerPlus.
/// Add as a component on the same GameObject as the player controller.
/// Override Score to control when this mode activates (higher score wins).
/// The default animation pipeline always runs; override UpdateAnimator to add extra animation on top.
/// </summary>
[Icon( "extension" )]
public abstract class ExternalMoveMode : Component
{
	public PlayerControllerPlus Controller { get; internal set; }

	/// <summary>
	/// Return a score to compete with built-in modes. Walk is 0, Ladder is 5, Swim is 10, Sit is 10000.
	/// Return negative to opt out.
	/// </summary>
	public abstract int Score( PlayerControllerPlus controller );

	public virtual bool AllowGrounding => false;
	public virtual bool AllowFalling => false;

	/// <summary>
	/// Whether the given trace result is a surface the player can stand on.
	/// Default allows any surface within 45 degrees of flat.
	/// </summary>
	public virtual bool IsStandableSurface( in SceneTraceResult result )
	{
		return result.Normal.Angle( Vector3.Up ) <= 45f;
	}

	/// <summary>
	/// Called when this mode becomes the active mode.
	/// </summary>
	public virtual void OnModeBegin() { }

	/// <summary>
	/// Called when this mode is being replaced by another mode.
	/// </summary>
	public virtual void OnModeEnd() { }

	/// <summary>
	/// Add velocity to the physics body. Default does nothing (no physics-driven movement).
	/// </summary>
	public virtual void AddVelocity() { }

	public virtual void PrePhysicsStep() { }
	public virtual void PostPhysicsStep() { }

	/// <summary>
	/// Configure the rigidbody for this mode. Default disables gravity and applies high damping.
	/// </summary>
	public virtual void UpdateRigidBody( Rigidbody body )
	{
		body.Gravity = false;
		body.LinearDamping = 10f;
		body.AngularDamping = 1f;
	}

	/// <summary>
	/// Process movement input and return a wish velocity. Default returns zero.
	/// </summary>
	public virtual Vector3 UpdateMove( Rotation eyes, Vector3 input ) => Vector3.Zero;

	/// <summary>
	/// Called after the default animation pipeline runs. Override to set extra animation parameters.
	/// </summary>
	public virtual void UpdateAnimator( SkinnedModelRenderer renderer ) { }

	/// <summary>
	/// Override to provide a custom eye transform. Return null to use the default calculation.
	/// </summary>
	public virtual Transform? GetEyeTransform() => null;
}
