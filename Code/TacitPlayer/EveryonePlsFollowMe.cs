namespace Sandbox;

public sealed class EveryonePlsFollowMe : Component
{
	// steal code from facepunch, again lol
	// https://github.com/Facepunch/sbox-scenestaging/blob/main/Code/ExampleComponents/NavigationQueryTest.cs
	[Property,ReadOnly] RealTimeSince updateAttractIn {get;set;} = 0;

	protected override void OnUpdate()
	{
		// If this component is enabled, make all agents in this scene attracted to whoever this component resides at.
		if(updateAttractIn > 1) // update every 1
		{
			updateAttractIn = 0;

			foreach(var agent in Scene.GetAllComponents<NavMeshAgent>())
			{
				if ( agent.IsValid() )
					agent.MoveTo( WorldPosition );

				// TODO: deprecate above and instead the NPC has component that handles all animation: model.Set("slider_name")
				// https://github.com/Facepunch/sbox-scenestaging/blob/main/Code/ExampleComponents/NavigationTargetWanderer.cs
			}
		}

	}
}
