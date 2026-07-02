namespace Sandbox;

public sealed class SaveGameApiTest : Component
{
	// https://sbox.game/dev/doc/assets/storage-ugc
	// Storage.Entry saveing { get; set; } = Storage.CreateEntry( "save" );
	Storage.Entry saveing { get; set; } = Storage.CreateEntry( "save" );

	protected override void OnStart()
	{
		base.OnStart();
		DataStructureSample data = new DataStructureSample("AAA BBB");
		//data.Name = "AAA BBB";
		// saveing.SetMeta( "playerName", "awawa" );
		// saveing.SetMeta( "someSome", "sdafasklfjklwejlk" );
		// saveing.SetMeta( "level", 12 );
		// saveing.Files.WriteAllText( "saveingTeswt.txt", "hello world" );
		// saveing.Files.WriteJson<DataStructureSample>( "dataStructTest.json", data );

		// How about this instead?
		// https://sbox.game/dev/doc/assets/file-system
		FileSystem.Data.WriteJson<DataStructureSample>( "dataStructTestRaw.json", data );
		FileSystem.OrganizationData.WriteJson<DataStructureSample>( "dataStructTestRaw.json", data );
		// so saveing is only for doom?

		//DataStructureSample readData = saveing.Files.ReadJson<DataStructureSample>( "dataStructTest.json" );
		//Log.Info( $"Here data:,\n\n{readData}" );

		var allSaves = Storage.GetAll( "save" );

		/*

		foreach ( var save in allSaves )
		{
    		// Access entry properties
		    Log.Info( $"Save: {save.Id}" );
		    Log.Info( $"Created: {save.Created}" );

		    // Read metadata
		    var playerName = save.GetMeta<string>( "playerName" );
		    var someSome = save.GetMeta<string>( "someSome" );
		    var level = save.GetMeta<int>( "level" );

		    // Access the thumbnail
		    var thumbnail = save.Thumbnail;

		    // Read files from the entry
		    if ( save.Files.FileExists( "player.json" ) )
		    {
		        var playerJson = save.Files.ReadAllText( "player.json" );
		        // Load your game...
		    }
		}
		*/
	}

	protected override void OnUpdate()
	{

	}
}
