namespace Sandbox;

public sealed class AnCookieClicker : Component
{
	// Cookie Clicker simple base
	[Property] public double Cookies { get; set; } = 0;
	[Property] public float Multiplier { get; set; } = 1f;

	public void ClickTheCookie()
	{
		// Cookies++;
		Cookies += Multiplier;
	}

	public void AddMultiplierBy( float Number )
	{
		Multiplier += Number;
	}

	public void ResetMultiplier()
	{
		Multiplier = 1f;
	}

	protected override void OnUpdate()
	{
		if ( Multiplier < 0.1f )
		{
			Multiplier = 0.1f;
		}

		// TODO: JSON save
	}
}
