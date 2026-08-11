namespace Sandbox;

public sealed class Npctry : Component
{
	[RequireComponent] public SkinnedModelRenderer Model { get; set; }
	[RequireComponent] public NavMeshAgent Agent { get; set; }
	[Property] public float StandbackBy { get; set; } = 200f;
	[Property] public GameObject Thorax { get; internal set; }
	// RealTimeSince updateNav;

	protected override void OnStart()
	{
		// try make a new empty gameobject?
		NewTorsoFolder();
	}

	protected override void OnUpdate()
	{
		// https://github.com/Facepunch/sbox-scenestaging/blob/main/Code/ExampleComponents/NavigationTargetWanderer.cs
		var dir = Agent.Velocity;
		var forward = WorldRotation.Forward.Dot( dir );
		var sideward = WorldRotation.Right.Dot( dir );

		var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

		Model.Set( "move_direction", angle );
		Model.Set( "move_speed", Agent.Velocity.Length );
		Model.Set( "move_groundspeed", Agent.Velocity.WithZ( 0 ).Length );
		Model.Set( "move_y", sideward );
		Model.Set( "move_x", forward );
		Model.Set( "move_z", Agent.Velocity.z );

		Model.Set( "wish_x", Agent.WishVelocity.x );
		Model.Set( "wish_y", Agent.WishVelocity.y );
		Model.Set( "wish_z", Agent.WishVelocity.z );

		// if(updateNav > 1f)
		// {
		if ( Agent.IsNavigating )
		{
			// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types
			float disToTarget = Vector3.DistanceBetween( WorldPosition, Agent.TargetPosition ?? Vector3.Zero );
			if ( disToTarget <= StandbackBy )
				Agent.Stop();

		}
		// updateNav = 0f;
		// }
	}

	public void GoToThisPosition( Vector3 whatPos )
	{
		if ( Agent.IsValid() )
			if ( Vector3.DistanceBetween( WorldPosition, Agent.TargetPosition ?? Vector3.Zero ) > StandbackBy )
				Agent.MoveTo( whatPos );
	}

	protected void NewTorsoFolder()
	{
		if ( !Networking.IsHost ) return;
		if ( !Thorax.IsValid() )
		{
			var NewFolder = new GameObject( GameObject, true, "Thorax" ); // parent to this npc, enabled, and name this folder "Thorax"
																		  //NewFolder.SetParent( GameObject, false );
			NewFolder.NetworkSpawn( Network.Owner );
			Thorax = NewFolder;
		}

		// You can now spawn heart organ inside this Thorax, e.g.
	}
}
