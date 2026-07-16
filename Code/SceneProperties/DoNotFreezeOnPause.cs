using Sandbox;

public sealed class DoNotFreezeOnPause : Component, ISceneProperties
{
	// add this component to scene to be detected.
	// when it found, StartMenu won't timescale
	[Property] protected StartMenu? theMenu { get; set; }

	protected override void OnStart()
	{
		if(!theMenu.IsValid())
		{
			try{
				GameObject? findThe = Scene.Directory.FindByName( "ScreenMenu" ).First();
				// theMenu = Scene.Directory.FindByName( "ScreenMenu" ).First().GetComponent<StartMenu>();
				if(findThe.IsValid()) theMenu = findThe.GetComponent<StartMenu>();
			} catch (Exception e)
			{

			}
		}
		if ( theMenu.IsValid() )
		{
			theMenu.PlsDoNotFreeze = true;
		}
	}

	protected override void OnUpdate()
	{

	}
}
