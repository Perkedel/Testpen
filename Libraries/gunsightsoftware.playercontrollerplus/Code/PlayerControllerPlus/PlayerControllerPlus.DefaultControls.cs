using System;

namespace Sandbox;

public sealed partial class PlayerControllerPlus : Component
{
	/// <summary>
	/// The direction we're looking in input space.
	/// </summary>
	[Sync( SyncFlags.Interpolate )]
	public Angles EyeAngles { get; set; }

	/// <summary>
	/// The player's eye position, in first person mode
	/// </summary>
	public Vector3 EyePosition => EyeTransform.Position;

	/// <summary>
	/// The player's eye transform, in first person mode
	/// </summary>
	public Transform EyeTransform { get; private set; }

	/// <summary>
	/// True if this player is ducking
	/// </summary>
	[Sync]
	public bool IsDucking { get; set; }

	/// <summary>
	/// The distance from the top of the head to the closest ceiling.
	/// </summary>
	public float Headroom { get; set; }


	Vector3 _proxyLastPosition;

	protected override void OnUpdate()
	{
		UpdateGroundEyeRotation();

		if ( Scene.IsEditor )
			return;

		if ( IsProxy && ExtendedFeaturesEnabled )
		{
			var pos = WorldPosition;
			Velocity = (pos - _proxyLastPosition) / Time.Delta;
			_proxyLastPosition = pos;
		}

		UpdateEyeTransform();

		if ( !IsProxy )
		{
			using var scope = InputPlayerIndex >= 0 ? Input.PlayerScope( InputPlayerIndex ) : null;

			GameObject.RunEvent<IEvents>( x => x.PreInput() );

			if ( UseLookControls && !(ExtendedFeaturesEnabled && DisableLookInput) )
			{
				UpdateEyeAngles();
				UpdateLookAt();
			}

			if ( UseCameraControls )
			{
				UpdateCameraPosition();
			}

			UpdateEyeTransform();
		}

		UpdateBodyVisibility();

		if ( UseAnimatorControls && Renderer.IsValid() )
		{
			UpdateAnimation( Renderer );
		}

		if ( ExtendedFeaturesEnabled && ShowStats && !IsProxy )
		{
			DrawStats();
		}
	}

	void DrawStats()
	{
		var pos = WorldPosition + Vector3.Up * (CurrentHeight + 10);
		var vel = Velocity;

		var state = GetMoveStateName();
		var grounded = IsOnGround ? "Grounded" : "Airborne";

		DebugOverlay.Text( pos, $"Vel X: {vel.x:F1}  Y: {vel.y:F1}\n{state}  |  {grounded}", size: 14, duration: 0, overlay: true );
	}

	string GetMoveStateName()
	{
		if ( Mode is Sandbox.MovementPlus.ExternalMoveModeAdapter adapter && adapter.External.IsValid() )
			return adapter.External.GetType().Name;

		if ( Mode is Sandbox.MovementPlus.NoclipMoveModePlus ) return "Noclip";
		if ( Mode is Sandbox.MovementPlus.SitMoveModePlus ) return "Sit";
		if ( Mode is Sandbox.MovementPlus.MoveModeSwimPlus ) return "Swim";
		if ( Mode is Sandbox.MovementPlus.MoveModeLadderPlus ) return "Ladder";

		if ( Mode is Sandbox.MovementPlus.MoveModeWalkPlus )
		{
			if ( IsDucking ) return "Crouch";
			if ( EnableSlowWalk && Input.Down( SlowWalkButton ) ) return "Slow Walk";

			bool run = Input.Down( AltMoveButton );
			if ( RunByDefault ) run = !run;
			return run ? "Run" : "Walk";
		}

		return Mode?.GetType().Name ?? "None";
	}

	protected override void OnFixedUpdate()
	{
		if ( Scene.IsEditor ) return;

		UpdateModeFixedUpdates();
		UpdateHeadroom();
		UpdateFalling();

		prevPosition = WorldPosition;

		if ( IsProxy ) return;
		if ( !UseInputControls ) return;

		using var fixedScope = InputPlayerIndex >= 0 ? Input.PlayerScope( InputPlayerIndex ) : null;

		InputMove();
		UpdateDucking( Input.Down( "duck" ) );
		InputJump();

		UpdateEyeTransform();
	}

	void UpdateHeadroom()
	{
		var tr = TraceBody( WorldPosition + Vector3.Up * CurrentHeight * 0.5f, WorldPosition + Vector3.Up * (100 + CurrentHeight * 0.5f), 0.75f, 0.5f );
		Headroom = tr.Distance;
	}

	bool _wasFalling = false;
	float fallDistance = 0;
	Vector3 prevPosition;
	internal Vector3? _landingVelocity;

	void UpdateFalling()
	{
		if ( Mode is null || !Mode.AllowFalling )
		{
			_wasFalling = false;
			fallDistance = 0;
			return;
		}

		if ( !IsOnGround || _wasFalling )
		{
			var fallDelta = WorldPosition - prevPosition;
			if ( fallDelta.z < 0.0f )
			{
				_wasFalling = true;
				fallDistance -= fallDelta.z;
			}
		}

		if ( IsOnGround )
		{
			if ( _wasFalling && fallDistance > 1.0f )
			{
				if ( ExtendedFeaturesEnabled && PreserveVelocityOnLanding )
				{
					_landingVelocity = Velocity.WithZ( 0 );
				}

				IEvents.PostToGameObject( GameObject, x => x.OnLanded( fallDistance, Velocity ) );

				// play land sounds
				if ( EnableFootstepSounds )
				{
					var volume = Velocity.Length.Remap( 50, 800, 0.5f, 5 );
					var vel = Velocity.Length;

					PlayFootstepSound( WorldPosition, volume, 0 );
					PlayFootstepSound( WorldPosition, volume, 1 );
				}
			}

			_wasFalling = false;
			fallDistance = 0;
		}
	}

	Transform localGroundTransform;
	int groundHash;

	void UpdateGroundEyeRotation()
	{
		if ( GroundObject is null )
		{
			groundHash = default;
			return;
		}

		if ( !RotateWithGround )
		{
			groundHash = default;
			return;
		}

		var hash = HashCode.Combine( GroundObject );

		// Get out transform locally to the ground object
		var localTransform = GroundObject.WorldTransform.ToLocal( WorldTransform );

		// Work out the rotation delta chance since last frame
		var delta = localTransform.Rotation.Inverse * localGroundTransform.Rotation;

		// we only care about the yaw
		var deltaYaw = delta.Angles().yaw;

		//DebugDrawSystem.Current.Text( WorldPosition, $"{delta.Angles().yaw}" );

		// If we're on the same ground and we've rotated
		if ( hash == groundHash && deltaYaw != 0 )
		{
			// rotate the eye angles
			EyeAngles = EyeAngles.WithYaw( EyeAngles.yaw + deltaYaw );

			// rotate the body to avoid it animating to the new position
			if ( UseAnimatorControls && Renderer.IsValid() )
			{
				Renderer.WorldRotation *= new Angles( 0, deltaYaw, 0 );
			}
		}

		// Keep for next frame
		groundHash = hash;
		localGroundTransform = localTransform;
	}



}
