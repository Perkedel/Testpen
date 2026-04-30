using Sandbox;

// I can't believe this, there's no standard `Health e Dedd` component here in s&box?!
// Fucking really? everybody (Gamemode itself) had to do this themselves?
// https://github.com/Facepunch/sbox-walker/blob/main/code/Player/Player.cs
// https://github.com/Facepunch/sandbox/blob/main/Code/Player/Player.cs
// Shuhck!! Ugh really!?!?!?!
// Josh!!! MAKE ME DOOM-QUAKE-LIKE HEALTH ARMOR COMPONENT SYSTEM!!!, Later after we're done here!
// For now, we're going to yoink between Walker & Sanbox gamemode Health component.

public sealed partial class TacitPlayerSample: Component, Component.IDamageable, PlayerController.IEvents
{
	// Tacit = Stealthily. Oh thancc God, not a trouble word.

	private static TacitPlayerSample Local { get; set; }
	// public static TacitPlayerSample FindLocalPlayer() => Local;
	public static TacitPlayerSample FindLocalPlayer() => Game.ActiveScene.GetAllComponents<TacitPlayerSample>().Where( x => !x.IsProxy ).FirstOrDefault();

	[Property, RequireComponent] public PlayerController Controller { get; set; } // Meant to be complement of Player Controller

	[Property, Range( 0, 100 ), Sync( SyncFlags.FromHost )] public float Health { get; set; } = 100;
	[Property, Range( 0, 100 ), Sync( SyncFlags.FromHost )] public float MaxHealth { get; set; } = 100;

	public bool IsDead => Health <= 0;

	/// <summary>
	/// Creates a ragdoll but it isn't enabled
	/// </summary>
	[Rpc.Broadcast]
	void CreateRagdoll()
	{
		var ragdoll = Controller.CreateRagdoll();
		if ( !ragdoll.IsValid() ) return;

		var corpse = ragdoll.AddComponent<TacitPlayerCorpse>();
		corpse.Connection = Network.Owner;
		corpse.Created = DateTime.Now;
	}

	[Rpc.Broadcast( NetFlags.OwnerOnly )]
	void CreateRagdollAndGhost()
	{
		if ( !Networking.IsHost ) return;

		var go = new GameObject( false, "Observer" );
		go.Components.Create<TacitPlayerObserver>();
		go.NetworkSpawn( Rpc.Caller );
	}

	public void TakeDamage( float amount )
	{
		if ( IsProxy ) return;
		if ( Health <= 0 ) return;

		Health -= amount;

		IPlayerEvent.PostToGameObject( GameObject, x => x.OnTakeDamage( amount ) );

		if ( Health <= 0 )
		{
			Health = 0;
			Death();
		}
	}

	void Death()
	{
		CreateRagdoll();
		CreateRagdollAndGhost();

		IPlayerEvent.PostToGameObject( GameObject, x => x.OnDied() );

		GameObject.Destroy();
	}

	void IDamageable.OnDamage( in DamageInfo damage )
	{
		Log.Info( $"Ouch {damage.Damage}" );
		TakeDamage( damage.Damage );
	}

	void PlayerController.IEvents.OnEyeAngles( ref Angles ang )
	{
		var player = Components.Get<TacitPlayerSample>();
		var angles = ang;
		ILocalPlayerEvent.Post( x => x.OnCameraMove( ref angles ) );
		ang = angles;
	}

	void PlayerController.IEvents.PostCameraSetup( CameraComponent camera )
	{
		var player = Components.Get<TacitPlayerSample>();
		ILocalPlayerEvent.Post( x => x.OnCameraSetup( camera ) );
		ILocalPlayerEvent.Post( x => x.OnCameraPostSetup( camera ) );
	}

	void PlayerController.IEvents.OnLanded( float distance, Vector3 impactVelocity )
	{
		var player = Components.Get<TacitPlayerSample>();
		IPlayerEvent.PostToGameObject( GameObject, x => x.OnLand( distance, impactVelocity ) );
	}
}
