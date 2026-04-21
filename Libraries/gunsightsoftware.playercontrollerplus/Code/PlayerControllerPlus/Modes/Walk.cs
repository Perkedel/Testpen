namespace Sandbox.MovementPlus;

public class MoveModeWalkPlus : MoveModePlus
{
	public override bool AllowGrounding => true;
	public override bool AllowFalling => true;
	public override bool UseLateralAirControl => true;

	public override int Score( PlayerControllerPlus controller ) => 0;

	public override void AddVelocity()
	{
		Controller.WishVelocity = Controller.WishVelocity.WithZ( 0 );
		base.AddVelocity();
	}

	public override void PrePhysicsStep()
	{
		base.PrePhysicsStep();

		if ( Controller.WalkStepUpHeight > 0 )
		{
			TrySteppingUp( Controller.WalkStepUpHeight );
		}
	}

	public override void PostPhysicsStep()
	{
		base.PostPhysicsStep();

		StickToGround( Controller.WalkStepDownHeight );
	}

	public override bool IsStandableSurface( in SceneTraceResult result )
	{
		if ( Vector3.GetAngle( Vector3.Up, result.Normal ) > Controller.WalkGroundAngle )
			return false;

		return true;
	}

	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		eyes = eyes.Angles() with { pitch = 0 };

		return base.UpdateMove( eyes, input );
	}
}
