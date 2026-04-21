namespace Sandbox.MovementPlus;

/// <summary>
/// Wraps an ExternalMoveMode component so it can participate in the MoveModePlus system.
/// </summary>
internal class ExternalMoveModeAdapter : MoveModePlus
{
	public ExternalMoveMode External { get; set; }

	public override bool AllowGrounding => External.IsValid() && External.AllowGrounding;
	public override bool AllowFalling => External.IsValid() && External.AllowFalling;

	public override bool IsStandableSurface( in SceneTraceResult result )
	{
		if ( External.IsValid() )
			return External.IsStandableSurface( result );

		return false;
	}

	public override int Score( PlayerControllerPlus controller )
	{
		if ( !External.IsValid() || !External.Enabled )
			return int.MinValue;

		return External.Score( controller );
	}

	public override void OnModeBegin() => External?.OnModeBegin();
	public override void OnModeEnd( MoveModePlus next ) => External?.OnModeEnd();

	public override void AddVelocity()
	{
		if ( External.IsValid() )
			External.AddVelocity();
	}

	public override void PrePhysicsStep()
	{
		if ( External.IsValid() )
			External.PrePhysicsStep();
	}

	public override void PostPhysicsStep()
	{
		if ( External.IsValid() )
			External.PostPhysicsStep();
	}

	public override void UpdateRigidBody( Rigidbody body )
	{
		if ( External.IsValid() )
			External.UpdateRigidBody( body );
		else
			base.UpdateRigidBody( body );
	}

	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		if ( External.IsValid() )
			return External.UpdateMove( eyes, input );

		return Vector3.Zero;
	}

	public override void UpdateAnimator( SkinnedModelRenderer renderer )
	{
		base.UpdateAnimator( renderer );

		if ( External.IsValid() )
			External.UpdateAnimator( renderer );
	}

	public override Transform CalculateEyeTransform()
	{
		if ( External.IsValid() )
		{
			var custom = External.GetEyeTransform();
			if ( custom.HasValue )
				return custom.Value;
		}

		return base.CalculateEyeTransform();
	}
}
