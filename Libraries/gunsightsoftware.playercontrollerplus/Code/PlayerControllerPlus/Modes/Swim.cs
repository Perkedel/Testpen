using System.Linq;

namespace Sandbox.MovementPlus;

public class MoveModeSwimPlus : MoveModePlus
{
	public float WaterLevel { get; private set; }

	public override void UpdateRigidBody( Rigidbody body )
	{
		body.Gravity = false;
		body.LinearDamping = 3.3f;
		body.AngularDamping = 1f;
	}

	public override int Score( PlayerControllerPlus controller )
	{
		if ( WaterLevel > controller.SwimLevel ) return controller.SwimPriority;
		return -100;
	}

	public override void OnModeBegin()
	{
		Controller.IsSwimming = true;
	}

	public override void OnModeEnd( MoveModePlus next )
	{
		Controller.IsSwimming = false;

		if ( Input.Down( "Jump" ) )
		{
			Controller.Jump( Vector3.Up * 300 );
		}
	}

	public override void FixedUpdate()
	{
		UpdateWaterLevel();
	}

	void UpdateWaterLevel()
	{
		if ( Controller?.Body == null )
			return;

		var wt = Controller.WorldTransform;
		Vector3 head = wt.PointToWorld( new Vector3( 0, 0, Controller.CurrentHeight ) );
		Vector3 foot = wt.Position;

		float waterLevel = 0;

		foreach ( var touch in Controller.Body.Touching )
		{
			if ( !touch.Tags.Contains( "water" ) ) continue;

			var waterSurface = touch.FindClosestPoint( head );
			var level = Vector3.InverseLerp( waterSurface, foot, head, true );
			level = (level * 100).CeilToInt() / 100.0f;

			if ( level > waterLevel )
				waterLevel = level;
		}

		if ( WaterLevel != waterLevel )
		{
			WaterLevel = waterLevel;
		}
	}

	public override Vector3 UpdateMove( Rotation eyes, Vector3 input )
	{
		if ( Input.Down( "jump" ) )
		{
			input += Vector3.Up;
		}

		return base.UpdateMove( eyes, input );
	}
}
