namespace Sandbox.MovementPlus;

public sealed class NoclipMoveModePlus : MoveModePlus
{
	protected override void OnUpdateAnimatorState( SkinnedModelRenderer renderer )
	{
		renderer.Set( "b_noclip", true );
		renderer.Set( "duck", 0f );
	}

	public override int Score( PlayerControllerPlus controller )
	{
		return controller.NoclipActive ? 1000 : -1;
	}

	public override void UpdateRigidBody( Rigidbody body )
	{
		body.Gravity = false;
		body.LinearDamping = 5.0f;
		body.AngularDamping = 1f;
	}

	public override void OnModeBegin()
	{
		Controller.IsClimbing = true;
		Controller.Body.Gravity = false;

		if ( !Controller.NoclipEnableCollision && Controller.ColliderObject.IsValid() )
			Controller.ColliderObject.Enabled = false;

		if ( !Controller.IsProxy )
			Sandbox.Services.Stats.Increment( "move.noclip.use", 1 );
	}

	public override void OnModeEnd( MoveModePlus next )
	{
		Controller.IsClimbing = false;
		Controller.Body.Velocity = Controller.Body.Velocity.ClampLength( Controller.RunSpeed );

		if ( Controller.ColliderObject.IsValid() )
			Controller.ColliderObject.Enabled = true;

		Controller.Renderer.Set( "b_noclip", false );
	}

	public override Transform CalculateEyeTransform()
	{
		var transform = base.CalculateEyeTransform();

		if ( Controller.IsDucking )
			transform.Position += Vector3.Up * (Controller.BodyHeight - Controller.DuckedHeight);

		return transform;
	}

	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		input = input.ClampLength( 1 );

		var direction = eyes * input;

		bool run = Input.Down( Controller.AltMoveButton );

		if ( Controller.RunByDefault ) run = !run;

		var velocity = run ? Controller.NoclipRunSpeed * 2.0f : Controller.NoclipRunSpeed;

		if ( Input.Down( "walk" ) ) velocity = Controller.NoclipWalkSpeed;

		if ( direction.IsNearlyZero( 0.1f ) )
		{
			direction = 0;
		}

		if ( Input.Down( "jump" ) ) direction += Vector3.Up;
		if ( Input.Down( "duck" ) ) direction += Vector3.Down;

		return direction * velocity;
	}

	public override void FixedUpdate()
	{
		if ( !Controller.NoclipToggleWithAirJump ) return;
		if ( Controller.IsProxy ) return;
		if ( !Input.Pressed( "Jump" ) ) return;

		if ( Controller.NoclipActive )
		{
			Controller.NoclipActive = false;
		}
		else if ( Controller.IsAirborne )
		{
			Controller.NoclipActive = true;
		}
	}
}
