// Stripped down https://github.com/Facepunch/sbox-walker/blob/main/code/GameManager.cs

namespace Sandbox;

public sealed class SceneBoss : Component
{
	[Property, ReadOnly] public PlayerRazorHUD AnHud { get; set; }

	protected override void OnStart()
	{
		// if(!AnHud.IsValid)
		{
			AnHud = Scene.Directory.FindByName( "PlayerHUD" ).First().GetComponent<PlayerRazorHUD>();
		}
	}

	protected override void OnUpdate()
	{

	}

	public void SpawnPlayerForConnection( Connection channel )
	{
		// Find a spawn location for this player
		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner
		var playerGo = GameObject.Clone( "/entity/enchantedplayercontroller/player controller edito.prefab", new CloneConfig { Name = $"Player - {channel.DisplayName}", StartEnabled = true, Transform = startLocation } );
		var player = playerGo.Components.Get<TacitPlayerSample>( true );
		var controller = playerGo.Components.Get<PlayerController>( true );
		if ( AnHud.IsValid )
		{
			AnHud.ReferedPlayer = controller;
			AnHud.HealthSystem = player;
		}
		playerGo.NetworkSpawn( channel );

		IPlayerEvent.PostToGameObject( player.GameObject, x => x.OnSpawned() );
	}

	/// <summary>
	/// Find the most appropriate place to respawn
	/// </summary>
	Transform FindSpawnLocation()
	{
		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).Transform.World;
		}

		//
		// Failing that, spawn where we are
		//
		return new Transform();
	}
}
