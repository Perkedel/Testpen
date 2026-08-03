namespace Sandbox;

public sealed class EveryonePlsFollowMe : Component
{
	// steal code from facepunch, again lol
	// https://github.com/Facepunch/sbox-scenestaging/blob/main/Code/ExampleComponents/NavigationQueryTest.cs
	[Property, ReadOnly] RealTimeSince updateAttractIn { get; set; } = 0;
	[Property, ReadOnly] public CharacterController[] caughtCharacters { get; internal set; }

	protected override void OnStart()
	{
		foreach (var charc in Scene.GetAllComponents<CharacterController>())
		{
			try
			{
				if ( charc.IsValid() )
					caughtCharacters.Append( charc );
				// Yes, I know, `Source` tab AI evaluator, would be nice you gave us solution
				// Just kidding, **nope**.
			}
			finally
			{

			}
			// fuck idk how, fuck this this, i fucking don't fucking knwo pdfksaldjhfalsjkd helppslfjsd
		}
	}

	protected override void OnUpdate()
	{
		// If this component is enabled, make all agents in this scene attracted to whoever this component resides at.
		if(updateAttractIn > 1) // update every 1
		{
			updateAttractIn = 0;

			foreach ( var agent in Scene.GetAllComponents<NavMeshAgent>() )
			{
				if ( agent.IsValid() )
				{
					agent.MoveTo( WorldPosition );
				}


				// TODO: deprecate above and instead the NPC has component that handles all animation: model.Set("slider_name")
				// https://github.com/Facepunch/sbox-scenestaging/blob/main/Code/ExampleComponents/NavigationTargetWanderer.cs
			}


			foreach (var charc in Scene.GetAllComponents<CharacterController>())
			{
				if (charc.IsValid())
				{
					// charc.MoveTo( WorldPosition, true );
					// charc.;
				}
			}
		}

	}
}
