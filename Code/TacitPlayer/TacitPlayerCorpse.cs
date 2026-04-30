// copy https://github.com/Facepunch/sbox-walker/blob/main/code/Player/PlayerCorpse.cs

﻿/// <summary>
/// A corpse. Clientside only. Automatically destroyed after a period of time.
/// </summary>
public class TacitPlayerCorpse : Component
{
	public Connection Connection { get; set; }
	public DateTime Created { get; set; }

	protected override void OnEnabled()
	{
		Invoke( 60.0f, GameObject.Destroy );
	}
}