using Goo;
using Sandbox.UI;

namespace Sandbox.UI.Basical;

public sealed class ProgressBarGoo : GooPanel<Container>
{
	[Property] float Value { get; set; } = 50f;
	[Property] float Minimum { get; set; } = 0f;
	[Property] float Maximum { get; set; } = 100f;

	public ProgressBarGoo( float height = 12f )
	{

	}

	public ProgressBarGoo()
	{
		
	}

	protected override void OnUpdate()
	{
		base.OnUpdate(); // critical crucial, forgor and it malfunctions
	}

	protected override Container Build() => new Container
	{
		BackgroundColor = Color.Gray,
		Children = {
			new Container{
				// Contains the bar
				BackgroundColor = Color.Black,
				Children = {
					// The bar
					new Container{
						BackgroundColor = Color.Red,
						Height = 100,
					}
				},
			},
			new Text("50%"),
		},
	};
}
