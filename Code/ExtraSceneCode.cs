using Sandbox;

// This is extra scene code sample. Again, let's learn s&box together, you and I!
// https://sbox.game/dev/doc/code/
// Contains each explanation copied from there!

public sealed class ExtraSceneCode : Component
{

	// We got more Methods from Component!
	// https://sbox.game/dev/doc/scene/components/component-methods
	protected override void OnStart()
	{
		// init!
		// Called when the component is enabled for the first time.
		// Should always get called before the first OnFixedUpdate.
		base.OnStart();

		// I forgor what was I originally add this one for 💀
		// Oh yeah, achievement yess.
		// https://sbox.game/dev/doc/services/achievements
		Sandbox.Services.Achievements.Unlock( "entered_first" );

		// File system?!
		// https://sbox.game/dev/doc/assets/file-system
		// https://asset.party/api/Sandbox.FileSystem.Data **GONE**
		if ( !FileSystem.Data.FileExists( "hello.txt" ) )
    		FileSystem.OrganizationData.WriteAllText( "minFolder/hello.txt", "Hello, world!" );

      	var hello = FileSystem.Data.ReadAllText( "hello.txt" );

		// https://sbox.game/dev/doc/scene/components/reference/temporaryeffect
	}

	protected override void OnUpdate()
	{
		// every frame!
		// Called every frame
		base.OnUpdate();
	}

	// Yeah you should know at least those above 2 signature methods
	// basically every game engine has.
	// Also you may not need to call each super methods (base.*), since they're empty anyway
	// and probably such bug coz forgor calling may have been fixed today.
	// Tho, if you inherit a custom component, you may still need call their super.
	// idk. can you comment on this as of now?

	protected override void OnFixedUpdate()
	{
		// Update but fixed rate.
		// Called every fixed timestep.
		// In general, it's wise to use a fixed update for things like player movement (the built in Character Controller does this).
		// This reduces the amount of traces a client is doing every frame, and if your client is too performant,
		// the move deltas per frame can be so small that they create problems.
	}

	protected override async Task OnLoad()
	{
		// modify the loading screen! you can also intentionally delay,
		// useful if you had an important instance you expect it
		// completed dl first.
		LoadingScreen.Title = "Prepping my guy..";
		//await Task.DelayRealtimeSeconds( 1.0f );
	}

	protected override void OnValidate()
	{
		// Deserialization!
		// Called whenever a property is changed in the editor, and after deserialization.
		// A good place to enforce property limits etc.
	}

	protected override void OnAwake()
	{
		// When this Component gets spawned anywhere it is, first time.
		// Basically before OnStart? & is Enabled
		// Called once when the component is created, but only if our parent GameObject is enabled.
		// This is called after deserialization and loading.
	}

	protected override void OnEnabled()
	{
		// literraly you Enabled it this.
		// Called when the component is enabled.
	}

	protected override void OnDisabled()
	{
		// literally you Disabled it this.
		// Called when the component is disabled.
	}

	protected override void OnPreRender()
	{
		// ==This method is not called on dedicated servers.==
		// Called every frame, right before rendering is about to take place.
		// This is called after animation bones have been calculated, so it usually a good place to do things that count on that.
	}

	protected override void OnDestroy()
	{
		// the destructor method, run when you destroy it this.
		// Called when the component is destroyed.
	}
}
