using System.Numerics;
using Sandbox;

public sealed class TrackballTest : Component, IGamepadTestBaunc
{
	[Property] public string expectedGlyphName { get; set; } = "";
	[Property] public GameObject baunc { get; set; }
	[Property] public float velocity { get; set; } = 5f;
	[Property] public GlyphImagePanel glyphImage { get; set; }

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

	public void SetAnalog(Rotation toBe)
	{
		if(baunc.IsValid())
		{
			baunc.LocalRotation = toBe;
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
