using System;

namespace Sandbox.MovementPlus;

/// <summary>
/// A move mode for this character. Not a Component — owned and managed by PlayerControllerPlus.
/// </summary>
public abstract partial class MoveModePlus
{
	public virtual bool AllowGrounding => false;
	public virtual bool AllowFalling => false;
	public virtual bool UseLateralAirControl => false;

	public PlayerControllerPlus Controller { get; set; }

	public virtual int Score( PlayerControllerPlus controller ) => 0;

	public virtual void PrePhysicsStep() { }
	public virtual void PostPhysicsStep() { }
	public virtual void FixedUpdate() { }

	public virtual void UpdateRigidBody( Rigidbody body )
	{
		bool wantsGravity = false;

		if ( !Controller.IsOnGround ) wantsGravity = true;
		if ( Controller.Velocity.Length > 1 ) wantsGravity = true;
		if ( Controller.GroundVelocity.Length > 1 ) wantsGravity = true;
		if ( Controller.GroundIsDynamic ) wantsGravity = true;

		body.Gravity = wantsGravity;

		bool wantsbrakes = Controller.IsOnGround && Controller.WishVelocity.Length < 1 && Controller.GroundVelocity.Length < 1;
		body.LinearDamping = wantsbrakes ? (10.0f * Controller.BrakePower) : Controller.AirFriction;

		body.AngularDamping = 1f;
	}

	public virtual void AddVelocity()
	{
		var body = Controller.Body;
		var wish = Controller.WishVelocity;
		if ( wish.IsNearZeroLength ) return;

		var groundFriction = 0.25f + Controller.GroundFriction * 10;
		var groundVelocity = Controller.GroundVelocity;

		var z = body.Velocity.z;

		var velocity = (body.Velocity - Controller.GroundVelocity);
		var speed = velocity.Length;

		var maxSpeed = MathF.Max( wish.Length, speed );

		if ( Controller.IsOnGround )
		{
			var amount = 1 * groundFriction;
			velocity = velocity.AddClamped( wish * amount, wish.Length * amount );
		}
		else
		{
			if ( UseLateralAirControl && Controller.ExtendedFeaturesEnabled )
			{
				var lateralWish = wish.WithZ( 0 );
				if ( !lateralWish.IsNearZeroLength )
				{
					var eyes = Controller.EyeAngles.ToRotation();
					var forward = eyes.Forward.WithZ( 0 ).Normal;
					var right = eyes.Right.WithZ( 0 ).Normal;

					var wishForward = lateralWish.Dot( forward );
					var wishRight = lateralWish.Dot( right );

					var forwardControl = wishForward >= 0 ? Controller.AirControlForward : Controller.AirControlBackward;
					var scaledWish = forward * wishForward * MathX.Lerp( 0.05f, 1f, forwardControl )
						+ right * wishRight * MathX.Lerp( 0.05f, 1f, Controller.AirControlLateral );

					var lateralVel = velocity.WithZ( 0 );
					lateralVel = lateralVel.AddClamped( scaledWish, scaledWish.Length );
					velocity = lateralVel.WithZ( velocity.z );
				}
			}
			else
			{
				var amount = 0.05f;
				velocity = velocity.AddClamped( wish * amount, wish.Length );
			}
		}

		if ( UseLateralAirControl && Controller.ExtendedFeaturesEnabled && !Controller.IsOnGround )
		{
			var lateral = velocity.WithZ( 0 );
			var lateralMax = MathF.Max( wish.WithZ( 0 ).Length, speed );
			if ( lateral.Length > lateralMax )
				lateral = lateral.Normal * lateralMax;
			velocity = lateral.WithZ( velocity.z );
		}
		else
		{
			if ( velocity.Length > maxSpeed )
				velocity = velocity.Normal * maxSpeed;
		}

		velocity += groundVelocity;

		if ( Controller.IsOnGround )
		{
			velocity.z = z;
		}

		body.Velocity = velocity;
	}

	public virtual void OnModeBegin() { }
	public virtual void OnModeEnd( MoveModePlus next ) { }

	protected void TrySteppingUp( float maxDistance )
	{
		Controller.TryStep( maxDistance );
	}

	protected void StickToGround( float maxDistance )
	{
		Controller.Reground( maxDistance );
	}

	public virtual bool IsStandableSurace( in SceneTraceResult result )
	{
		return false;
	}

	public virtual bool IsStandableSurface( in SceneTraceResult result )
	{
		return IsStandableSurace( result );
	}

	Vector3.SmoothDamped smoothedMovement;

	public virtual Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		input = input.ClampLength( 1 );

		var direction = eyes * input;

		bool run = Input.Down( Controller.AltMoveButton );

		if ( Controller.RunByDefault ) run = !run;

		var velocity = run ? Controller.RunSpeed : Controller.WalkSpeed;

		if ( !run && Controller.ExtendedFeaturesEnabled && Controller.EnableSlowWalk && Input.Down( Controller.SlowWalkButton ) )
			velocity = Controller.SlowWalkSpeed;

		if ( Controller.IsDucking ) velocity = Controller.DuckedSpeed;

		if ( direction.IsNearlyZero( 0.1f ) )
		{
			direction = 0;
		}
		else
		{
			smoothedMovement.Current = direction.Normal * smoothedMovement.Current.Length;
		}

		if ( Controller.ExtendedFeaturesEnabled && Controller._landingVelocity.HasValue )
		{
			smoothedMovement.Current = Controller._landingVelocity.Value;
			Controller._landingVelocity = null;
		}
		else
		{
			Controller._landingVelocity = null;
		}

		smoothedMovement.Target = direction * velocity;
		smoothedMovement.SmoothTime = smoothedMovement.Target.Length < smoothedMovement.Current.Length ? Controller.DeaccelerationTime : Controller.AccelerationTime;
		smoothedMovement.Update( Time.Delta );

		if ( smoothedMovement.Current.IsNearlyZero( 0.01f ) )
		{
			smoothedMovement.Current = 0;
		}

		return smoothedMovement.Current;
	}
}
