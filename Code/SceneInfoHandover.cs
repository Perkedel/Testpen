using Microsoft.VisualBasic;

namespace Sandbox;

public sealed class SceneInfoHandover : Component
{
	[Property] public SceneInformation ObtainSceneInfo { get; set; }
	[Property] public StartMenu? theMenu {get;set;}

	protected override void OnStart()
	{
		//ObtainSceneInfo = Scene.Directory.FindByName("Scene Information").First().GetComponent<SceneInformation>();
		if ( !ObtainSceneInfo.IsValid() )
		{
			try
			{
				GameObject? findThe = Scene.Directory.FindByName( "Scene Information" ).First();
				if ( findThe.IsValid() ) ObtainSceneInfo = findThe.GetComponent<SceneInformation>();

				if ( ObtainSceneInfo.IsValid() )
					Log.Info( $"Scene Here: {ObtainSceneInfo.Title}" );
			}
			catch ( Exception e )
			{

			}
			//ObtainSceneInfo = Scene.Directory.FindByName("Scene Information").First().GetComponent<SceneInformation>();
			//if(findThe.IsValid()) ObtainSceneInfo = findThe.GetComponent<SceneInformation>();
		}

		// damn! coding is haard, is this luck gonna last tho?

		if ( !theMenu.IsValid() )
		{
			try
			{
				GameObject? findThe = Scene.Directory.FindByName( "ScreenMenu" ).First();
				// theMenu = Scene.Directory.FindByName( "ScreenMenu" ).First().GetComponent<StartMenu>();
				if ( findThe.IsValid() ) theMenu = findThe.GetComponent<StartMenu>();
			}
			catch ( Exception e )
			{

			}
		}

		if (theMenu.IsValid())
		{
			// theMenu.ObtainSceneInfo = ObtainSceneInfo;
			theMenu.SubmitSceneInfo( ObtainSceneInfo );
		}
	}

	protected override void OnUpdate()
	{

	}
}
