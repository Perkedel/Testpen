using Sandbox;

public sealed class AnalogStickTest : Component, IGamepadTestBaunc
{
	[Property] public string expectedGlyphName { get; set; } = "";
	[Property] public GameObject baunc { get; set; }
	[Property] public GlyphImagePanel glyphImage { get; set; }
	[Property] public float radius { get; set; } = 10f;
	[Property] public float velocity { get; set; } = 5f;

	protected override void OnStart()
	{

	}

	protected override void OnUpdate()
	{
		if(glyphImage.IsValid())
		{
			glyphImage.ExpectedGlyphName = expectedGlyphName;
		}
	}

	public void SetAnalog( Vector2 toBe )
	{
		if ( baunc.IsValid() )
		{
			//baunc.Transform.LerpTo( new Vector3(toBe.x,toBe.y,32f), 1f );
			// baunc.Transform.Position = new Vector3( toBe.x, toBe.y, 32f ); // old, use bellow instead!
			baunc.LocalPosition = new Vector3( toBe.x * radius, toBe.y * radius, 32f );
		}
	}

	public void SetAnalog( Vector3 toBe, float offsetHeight = 32f )
	{
		if ( baunc.IsValid() )
		{
			baunc.LocalPosition = toBe * radius + Vector3.Up * offsetHeight;
		}
	}
	
	public void SetSquish(float toBe)
	{
		if(baunc.IsValid())
		{
			baunc.WorldScale = new Vector3( 1f, 1f, Math.Clamp( 1-toBe, .1f, 1f ) );
		}
	}
	
	public void SpinAnalog(Rotation toBe)
	{
		if(baunc.IsValid())
		{
			//baunc.LocalRotation += toBe * velocity;
			baunc.WorldRotation *= toBe * velocity;
		}
	}
}
