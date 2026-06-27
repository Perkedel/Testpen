using Goo;
using Sandbox.UI;
//using Sandbox.UI;


namespace Sandbox;

public sealed class Ecgoo : GooPanel<Container>
{
	
	protected override void OnUpdate()
	{
		base.OnUpdate(); // critical crucial, forgor and it malfunctions
	}

	protected override Container Build() => new Container
	{
		//Padding = 24,
		//BackgroundColor = Color.White,
		//BorderRadius = 12,
		PointerEvents = PointerEvents.All,
	 	FlexDirection = FlexDirection.Column,
		Children = { new Text( "Hello" ) },
	};
}
