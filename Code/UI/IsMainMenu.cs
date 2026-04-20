using Sandbox;

public sealed class IsMainMenu : Component
{
	[Property] protected StartMenu? theMenu {get;set;}
	[Property] protected DoNotFreezeOnPause? noFreeze {get;set;}

	protected override void OnStart()
	{
		if(!theMenu.IsValid())
		{
			theMenu = Scene.Directory.FindByName( "ScreenMenu" ).First().GetComponent<StartMenu>();
		}
		if ( theMenu.IsValid() )
		{
			theMenu.isPaused = true;
			theMenu.isInStart = true;
		}

		if(noFreeze.IsValid())
		{
			//Scene.TimeScale = 0f;
		}
	}

	protected override void OnUpdate()
	{

	}
}
