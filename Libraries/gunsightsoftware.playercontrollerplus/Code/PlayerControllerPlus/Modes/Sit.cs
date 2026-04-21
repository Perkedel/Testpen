namespace Sandbox.MovementPlus;

public sealed class SitMoveModePlus : MoveModePlus
{
	public override int Score( PlayerControllerPlus controller )
	{
		if ( controller.GetComponentInParent<ISitTarget>() is ISitTarget chair )
		{
			return 10000;
		}

		return -1;
	}

	public override void UpdateAnimator( SkinnedModelRenderer renderer )
	{
		if ( Controller.GetComponentInParent<ISitTarget>() is not ISitTarget chair )
			return;

		OnUpdateAnimatorVelocity( renderer );
		chair.UpdatePlayerAnimator( Controller, renderer );
	}

	public override void OnModeBegin()
	{
		base.OnModeBegin();

		Controller.Body.Enabled = false;
		Controller.ColliderObject.Enabled = false;
		Controller.EyeAngles = default;
	}

	public override void OnModeEnd( MoveModePlus next )
	{
		Controller.Body.Enabled = true;
		Controller.ColliderObject.Enabled = true;

		Controller.WorldRotation = Rotation.LookAt( Controller.EyeTransform.Forward.WithZ( 0 ), Vector3.Up );

		base.OnModeEnd( next );
	}

	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		return 0;
	}

	public override Transform CalculateEyeTransform()
	{
		if ( Controller.GetComponentInParent<ISitTarget>() is not ISitTarget chair )
		{
			return base.CalculateEyeTransform();
		}

		return chair.CalculateEyeTransform( Controller );
	}

	public void OnFailPressing()
	{
		if ( Controller.GetComponentInParent<ISitTarget>() is not ISitTarget chair )
			return;

		chair.AskToLeave( Controller );
	}
}

/// <summary>
/// A component that can be sat in by a player. If the player is parented to an object with this component, they will be sitting in it.
/// </summary>
public interface ISitTarget
{
	void UpdatePlayerAnimator( PlayerControllerPlus controller, SkinnedModelRenderer renderer );
	Transform CalculateEyeTransform( PlayerControllerPlus controller );
	void AskToLeave( PlayerControllerPlus controller );
}
