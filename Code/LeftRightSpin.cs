namespace Sandbox;

public sealed class LeftRightSpin : Component
{
	/*
	GARRY!!! YOU FUCKING BROKE CODE COMPONENT EXPORT IN PREFAB!!!
	WHERE THE FUCK IS MY DLL FUCKING FILES?!?!??!?!?!

	"MissingComponent"!??!?!? YOU THINK THIS IS SICK KICK, WHEN YOU CHANGED SOURCE CODE WORKFLOW?!
	Don't get me wrong tho, It's tiring ass to recompile every single time, & I am always into Open Source,
	BUT C'MON BRUH!
	THE FUCKING DLL EXPORT!! WHERE THE FUCK IS THAT?!?!? ATLEAST FUCKING GIVE THE PRECOMPILED DLL
	PER EACH COMPONENT THERE IS FUCKING TO IT!!! FOR FUCK SAKE!!!
	*/
	[Property] public float conjureAngularForceFor { get; set; } = 25000f;
	[Property, RequireComponent] public Rigidbody rigidItself { get; set; }
	[Property] public bool forceLeft { get; set; } = false;
	[Property] public bool forceRight { get; set; } = false;

	protected override void OnUpdate()
	{
		if ( /*Input.Down( "Left" ) ||*/ forceLeft)
		{
			if(rigidItself.IsValid())
			{
				rigidItself.ApplyTorque( LocalRotation * Vector3.Up * conjureAngularForceFor );
			}
		}
		if ( /*Input.Down("Right") ||*/ forceRight)
		{
			if(rigidItself.IsValid())
			{
				rigidItself.ApplyTorque( LocalRotation * Vector3.Up * -conjureAngularForceFor );
			}
		}
	}
}
