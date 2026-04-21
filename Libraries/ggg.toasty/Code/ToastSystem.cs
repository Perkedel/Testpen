public class ToastsSystem : GameObjectSystem
{
	public ToastsSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.FinishFixedUpdate, 0, SetupGlobals, "SetupGlobals" );
	}

	void SetupGlobals()
	{
		var cam = Game.ActiveScene.Camera;
		if ( !cam.IsValid() )
			return;
		cam.GetOrAddComponent<ToastsDisplay>();
		cam.GetOrAddComponent<ScreenPanel>();
		Dispose();
	}
}
