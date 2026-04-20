using Sandbox;

public sealed class SquishTest : Component, IGamepadTestBaunc
{
	// Can be confused with Breast or Pimple
	[Property] public string expectedGlyphName { get; set; } = "";
	[Property] public GameObject baunc { get; set; }
	[Property] public GlyphImagePanel glyphImage { get; set; }

	protected override void OnUpdate()
	{
		if(glyphImage.IsValid())
		{
			glyphImage.ExpectedGlyphName = expectedGlyphName;
		}
	}

	public void SetAnalog(float scaleBe)
	{
		//when 0, scale 1f, unsquish.
		// when 1, scale .1 squish
		// ai stfu lemme do this! You too, zed!
		if(baunc.IsValid())
		{
			//baunc.LocalScale = Math.Clamp( scaleBe, 1f, .1f );
			// baunc.LocalScale = Math.Clamp( scaleBe, .1f, 1f );
			// baunc.WorldScale = new Vector3( 1f, 1f, scaleBe );
			baunc.WorldScale = new Vector3( 1f, 1f, Math.Clamp( 1-scaleBe, .1f, 1f ) );
		}
	}
}
