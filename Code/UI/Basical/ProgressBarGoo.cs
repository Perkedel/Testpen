using Goo;
using Sandbox.UI;

namespace UI.Basical;

public sealed class ProgressBarGoo : GooPanel<Container>
{
	[Property] float Value { get; set; } = 50f;
	[Property] float Minimum { get; set; } = 0f;
	[Property] float Maximum { get; set; } = 100f;

	public ProgressBarGoo( float height = 12f)
	{

	}

	protected override void OnUpdate()
	{
		base.OnUpdate(); // critical crucial, forgor and it malfunctions
	}

	protected override Container Build() => new Container
	{
		BackgroundColor = Color.Gray;
		Children = {
			new Container{
				// filler
				Children = {},
			},
			new Text("50%"),
		},
	};
}
