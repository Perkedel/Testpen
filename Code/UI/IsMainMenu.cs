using Sandbox;

public sealed class IsMainMenu : Component
{
	[Property] protected StartMenu? theMenu {get;set;}

	protected override void OnStart()
	{
		if(!theMenu.IsValid())
		{
			theMenu = Scene.Directory.FindByName( "ScreenMenu" ).First().GetComponent<StartMenu>();
		}
		if(theMenu.IsValid())
		{
			theMenu.isPaused = true;
			theMenu.isInStart = true;
		}
	}

	protected override void OnUpdate()
	{

	}
}
