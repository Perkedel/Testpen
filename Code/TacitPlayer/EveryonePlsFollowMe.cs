namespace Sandbox;

public sealed class EveryonePlsFollowMe : Component
{
	// steal code from facepunch, again lol
	// https://github.com/Facepunch/sbox-scenestaging/blob/main/Code/ExampleComponents/NavigationQueryTest.cs
	[Property, ReadOnly] RealTimeSince updateAttractIn { get; set; } = 0;
	[Property, ReadOnly] public CharacterController[] caughtCharacters { get; internal set; }
	[Property, ReadOnly] public Npctry[] caughtNPCs { get; internal set; }
	[Property] public float personalSpaceRadius { get; set; } = 120f;

	protected override void OnStart()
	{
		/*

		foreach ( var charc in Scene.GetAllComponents<CharacterController>() )
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

		foreach( var npc in Scene.GetAllComponents<Npctry>())
		{
			try
			{
				if ( npc.IsValid() )
					caughtNPCs.Append( npc );
			} finally {

			}

		}
		*/
	}

	protected override void OnUpdate()
	{
		// If this component is enabled, make all agents in this scene attracted to whoever this component resides at.
		if(updateAttractIn > 1) // update every 1
		{
			updateAttractIn = 0;

			/*
			foreach ( var agent in Scene.GetAllComponents<NavMeshAgent>() )
			{
				if ( agent.IsValid() )
				{
					agent.MoveTo( WorldPosition );
					//Model.Set

					//DONE: Make them stop if close to me!
					// Vector3 thePos = agent.WorldPosition.WithZ( 0 );
					// Vector3 myPos = WorldPosition.WithZ( 0 );
					// float dis = Vector3.DistanceBetween( thePos, myPos );
					// if ( dis > personalSpaceRadius )
					// 	agent.MoveTo( WorldPosition );
					// else
					// {
					// 	agent.Stop();
					// }
				}


				// DONE: deprecate above and instead the NPC has component that handles all animation: model.Set("slider_name")
				// https://github.com/Facepunch/sbox-scenestaging/blob/main/Code/ExampleComponents/NavigationTargetWanderer.cs
			}
			*/

			foreach (var npc in Scene.GetAllComponents<Npctry>())
			{
				// Log.Info( $"hah {npc}" );
				if(npc.IsValid())
				{
					npc.GoToThisPosition( WorldPosition );
				}
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
