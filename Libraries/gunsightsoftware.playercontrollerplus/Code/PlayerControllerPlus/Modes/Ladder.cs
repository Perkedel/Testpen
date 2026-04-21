namespace Sandbox.MovementPlus;

public class MoveModeLadderPlus : MoveModePlus
{
	public GameObject ClimbingObject { get; set; }
	public Rotation ClimbingRotation { get; set; }

	public override void UpdateRigidBody( Rigidbody body )
	{
		body.Gravity = false;
		body.LinearDamping = 20.0f;
		body.AngularDamping = 1.0f;
	}

	public override int Score( PlayerControllerPlus controller )
	{
		if ( ClimbingObject.IsValid() ) return controller.LadderPriority;
		return -100;
	}

	public override void OnModeBegin()
	{
		Controller.IsClimbing = true;
		Controller.Body.Velocity = 0;
	}

	public override void OnModeEnd( MoveModePlus next )
	{
		Controller.IsClimbing = false;
		Controller.Body.Velocity = Controller.Body.Velocity.ClampLength( Controller.RunSpeed );
	}

	public override void PostPhysicsStep()
	{
		UpdatePositionOnLadder();
	}

	void UpdatePositionOnLadder()
	{
		if ( !ClimbingObject.IsValid() ) return;

		var pos = Controller.WorldPosition;

		var ladderPos = ClimbingObject.WorldPosition;
		var ladderUp = ClimbingObject.WorldRotation.Up;

		Line ladderLine = new Line( ladderPos - ladderUp * 1000, ladderPos + ladderUp * 1000 );

		var idealPos = ladderLine.ClosestPoint( pos );

		var delta = (idealPos - pos);
		delta = delta.SubtractDirection( ClimbingObject.WorldRotation.Forward );

		if ( delta.Length > 0.01f )
		{
			Controller.Body.Velocity = Controller.Body.Velocity.AddClamped( delta * 5.0f, delta.Length * 10.0f );
		}
	}

	public override void FixedUpdate()
	{
		ScanForLadders();
	}

	void ScanForLadders()
	{
		if ( Controller?.Body == null )
			return;

		var wt = Controller.WorldTransform;
		Vector3 head = wt.PointToWorld( new Vector3( 0, 0, Controller.CurrentHeight ) );
		Vector3 foot = wt.Position;

		GameObject ladderObject = default;

		foreach ( var touch in Controller.Body.Touching )
		{
			if ( !touch.Tags.HasAny( Controller.LadderClimbableTags ) )
				continue;

			if ( ClimbingObject == touch.GameObject )
			{
				ladderObject = touch.GameObject;
				continue;
			}

			var ladderSurface = touch.FindClosestPoint( head );
			var level = Vector3.InverseLerp( ladderSurface, foot, head, true );

			if ( ClimbingObject != touch.GameObject && level < 0.5f )
				continue;

			ladderObject = touch.GameObject;
			break;
		}

		if ( ladderObject == ClimbingObject )
			return;

		ClimbingObject = ladderObject;

		if ( ClimbingObject.IsValid() )
		{
			var directionToLadder = ClimbingObject.WorldPosition - Controller.WorldPosition;

			ClimbingRotation = ClimbingObject.WorldRotation;

			if ( directionToLadder.Dot( ClimbingRotation.Forward ) < 0 )
			{
				ClimbingRotation *= new Angles( 0, 180, 0 );
			}
		}
	}

	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		var wishVelocity = new Vector3( 0, 0, Input.AnalogMove.x );
		if ( eyes.Pitch() > 50f )
		{
			wishVelocity *= -1f;
		}

		wishVelocity *= 1500.0f * Controller.LadderSpeed * (Controller.IsDucking ? 0.5f : 1f);

		if ( Input.Down( "jump" ) )
		{
			Controller.Jump( ClimbingRotation.Backward * 200 );
		}

		return wishVelocity;
	}

	protected override void OnRotateRenderBody( SkinnedModelRenderer renderer )
	{
		renderer.WorldRotation = Rotation.Lerp( renderer.WorldRotation, ClimbingRotation, Time.Delta * 5.0f );
	}
}
