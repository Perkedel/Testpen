namespace Sandbox;

public sealed class TempDice : Component
{
	/*
	Lite version of WahahaDice, because the code does not fucking export through Prefabbing!
	*/
	[Property] public float conjureImpactFor { get; set; } = 100000000f;
	[Property] public float conjureRotationFor { get; set; } = 100000f;
	[Property, RequireComponent] public Rigidbody rigidItself { get; set; }

	protected override void OnUpdate()
	{

	}

	public void GiveImpact()
	{
		if(rigidItself.IsValid())
		{
			var rnd = new Random();
			rigidItself.ApplyImpulse( Vector3.Up * conjureImpactFor * (25f * rigidItself.Mass) );
			rigidItself.ApplyTorque( new Vector3(rnd.NextSingle() * conjureRotationFor,rnd.NextSingle() * conjureRotationFor,rnd.NextSingle() * conjureRotationFor) );
		}
	}
}
