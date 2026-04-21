namespace Sandbox.MovementPlus;

partial class MoveModePlus
{
	public virtual Transform CalculateEyeTransform()
	{
		if ( Controller.ExtendedFeaturesEnabled
			&& Controller.TrueFirstPerson
			&& !Controller.ThirdPerson
			&& !Controller.IsProxy
			&& Controller.Renderer.IsValid() )
		{
			if ( Controller.Renderer.TryGetBoneTransform( "head", out var headTx ) )
			{
				var rot = Controller.EyeAngles.ToRotation();
				var offset = rot * Controller.TrueFirstPersonOffset;
				var transform = new Transform();
				transform.Position = headTx.Position + offset;
				transform.Rotation = rot;
				return transform;
			}
		}

		var fallback = new Transform();
		fallback.Position = Controller.WorldPosition + Vector3.Up * (Controller.CurrentHeight - Controller.EyeDistanceFromTop);
		fallback.Rotation = Controller.EyeAngles.ToRotation();
		return fallback;
	}

	public void UpdateCamera( CameraComponent cam )
	{

	}
}
