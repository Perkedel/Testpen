using System;

namespace Sandbox.MovementPlus;

partial class MoveModePlus
{
	public virtual void UpdateAnimator( SkinnedModelRenderer renderer )
	{
		OnUpdateAnimatorVelocity( renderer );
		OnUpdateAnimatorState( renderer );
		OnUpdateAnimatorLookDirection( renderer );

		UpdateRotationSpeed( renderer );

		OnRotateRenderBody( renderer );
	}

	Vector3.SmoothDamped smoothedMove = new Vector3.SmoothDamped( 0, 0, 0.5f );
	Vector3.SmoothDamped smoothedWish = new Vector3.SmoothDamped( 0, 0, 0.5f );
	Vector3.SmoothDamped smoothedSkid = new Vector3.SmoothDamped( 0, 0, 0.5f );

	protected virtual void OnUpdateAnimatorVelocity( SkinnedModelRenderer renderer )
	{
		var rot = renderer.WorldRotation;
		var vel = Controller.Velocity;
		var wishVel = Controller.WishVelocity;

		// skid
		{
			var skidAmount = 0.5f;
			var pushSkidAmount = 1.0f;
			var skidDelay = 0.2f;

			var smoothed = vel;
			{
				smoothedMove.Target = smoothed;
				smoothedMove.SmoothTime = skidDelay;
				smoothedMove.Update( Time.Delta );
				smoothed = smoothedMove.Current;
			}

			var skid = (smoothed - vel) * skidAmount;

			skid = Vector3.Lerp( skid, vel * pushSkidAmount, wishVel.Length.Remap( 100, 0, 0, 1 ) );

			smoothedSkid.Target = skid;
			smoothedSkid.SmoothTime = 0.5f;
			smoothedSkid.Update( Time.Delta );
			skid = smoothedSkid.Current;

			skid = GetLocalVelocity( rot, skid );

			renderer.Set( "skid_x", (skid.x / 400.0f) );
			renderer.Set( "skid_y", (skid.y / 400.0f) );
		}

		// move the legs
		{
			var smoothed = (Controller.ExtendedFeaturesEnabled && Controller.UseWorldVelocityForAnimation) ? vel : wishVel;
			{
				smoothedWish.Target = smoothed;
				smoothedWish.SmoothTime = Controller.ExtendedFeaturesEnabled ? Controller.AnimationSmoothTime : 0.6f;
				smoothedWish.Update( Time.Delta );
				smoothed = smoothedWish.Current;

				smoothed = ApplyDeadZone( smoothed, 10 );
			}

			smoothed = GetLocalVelocity( rot, smoothed );

			renderer.Set( "move_direction", GetAngle( smoothed ) );
			renderer.Set( "move_speed", smoothed.Length );
			renderer.Set( "move_groundspeed", smoothed.WithZ( 0f ).Length );
			renderer.Set( "move_x", smoothed.x );
			renderer.Set( "move_y", smoothed.y );
			renderer.Set( "move_z", smoothed.z );
		}

		// the player's wish
		{
			var local = GetLocalVelocity( rot, wishVel );

			renderer.Set( "wish_direction", GetAngle( local ) );
			renderer.Set( "wish_speed", wishVel.Length );
			renderer.Set( "wish_groundspeed", wishVel.WithZ( 0f ).Length );
			renderer.Set( "wish_x", local.x );
			renderer.Set( "wish_y", local.y );
			renderer.Set( "wish_z", local.z );
		}

	}

	#region OnUpdateAnimatorVelocity Helpers

	private static Vector3 ApplyDeadZone( Vector3 velocity, float minimum )
	{
		return velocity.IsNearlyZero( minimum ) ? 0f : velocity;
	}

	private static Vector3 GetLocalVelocity( Rotation rotation, Vector3 worldVelocity )
	{
		var forward = rotation.Forward.Dot( worldVelocity );
		var sideward = rotation.Right.Dot( worldVelocity );

		return new Vector3( forward, sideward, worldVelocity.z );
	}

	private static float GetAngle( Vector3 localVelocity )
	{
		return MathF.Atan2( localVelocity.y, localVelocity.x ).RadianToDegree().NormalizeDegrees();
	}

	#endregion

	float _smoothedDuck;

	protected virtual void OnUpdateAnimatorState( SkinnedModelRenderer renderer )
	{
		renderer.Set( "sit", 0 );
		renderer.Set( "b_swim", Controller.IsSwimming );
		renderer.Set( "b_climbing", Controller.IsClimbing );
		renderer.Set( "b_grounded", Controller.IsOnGround || Controller.IsClimbing );

		var duck = Controller.Headroom.Remap( 25, 0, 0, 0.5f, true );

		if ( Controller.IsDucking )
		{
			duck *= 3.0f;
			duck += 1.0f;
		}

		if ( Controller.ExtendedFeaturesEnabled )
		{
			_smoothedDuck = _smoothedDuck.LerpTo( duck, Time.Delta * Controller.CrouchAnimationSpeed );
		}
		else
		{
			_smoothedDuck = duck;
		}

		renderer.Set( "duck", _smoothedDuck );
	}

	protected virtual void OnUpdateAnimatorLookDirection( SkinnedModelRenderer renderer )
	{
		var eyesForward = Controller.EyeTransform.Forward;

		renderer.SetLookDirection( "aim_eyes", eyesForward, Controller.AimStrengthEyes );
		renderer.SetLookDirection( "aim_head", eyesForward, Controller.AimStrengthHead );
		renderer.SetLookDirection( "aim_body", eyesForward, Controller.AimStrengthBody );
	}

	protected virtual void OnRotateRenderBody( SkinnedModelRenderer renderer )
	{
		var velocity = Controller.WishVelocity.WithZ( 0 );
		var oldRotation = renderer.WorldRotation;

		bool unbound = Controller.ExtendedFeaturesEnabled && Controller.DisableCameraBinding;

		Rotation targetAngle;

		if ( unbound )
		{
			if ( velocity.Length > 10 )
				targetAngle = Rotation.LookAt( velocity.Normal, Vector3.Up );
			else
				targetAngle = renderer.WorldRotation;
		}
		else
		{
			var eyeAngles = Controller.EyeTransform.Rotation.Angles();
			targetAngle = Rotation.FromYaw( eyeAngles.yaw );
		}

		var rotateDifference = renderer.WorldRotation.Distance( targetAngle );

		if ( rotateDifference > Controller.RotationAngleLimit )
		{
			var delta = 0.999f - Controller.RotationAngleLimit / rotateDifference;
			var newRotation = Rotation.Lerp( renderer.WorldRotation, targetAngle, delta );

			renderer.WorldRotation = newRotation;
		}

		if ( velocity.Length > 10 )
		{
			renderer.WorldRotation = Rotation.Slerp( renderer.WorldRotation, targetAngle,
				Time.Delta * 2.0f * Controller.RotationSpeed * velocity.Length.Remap( 0, 100 ) );
		}

		AddRotationSpeed( oldRotation, renderer.WorldRotation );
	}

	private const float RotationSpeedUpdatePeriod = 0.1f;

	private float _animRotationSpeed;
	private TimeSince _timeSinceRotationSpeedUpdate;

	private void AddRotationSpeed( Rotation oldRotation, Rotation newRotation )
	{
		var oldYaw = oldRotation.Angles().yaw;
		var newYaw = newRotation.Angles().yaw;

		var deltaYaw = MathX.DeltaDegrees( newYaw, oldYaw );

		_animRotationSpeed = (_animRotationSpeed + deltaYaw).Clamp( -90f, 90f );
	}

	private void UpdateRotationSpeed( SkinnedModelRenderer renderer )
	{
		if ( _timeSinceRotationSpeedUpdate < RotationSpeedUpdatePeriod ) return;

		renderer.Set( "move_rotationspeed", _animRotationSpeed * 5f );

		_timeSinceRotationSpeedUpdate = 0f;
		_animRotationSpeed = 0f;
	}

}
