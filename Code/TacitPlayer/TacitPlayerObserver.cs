// Copy https://github.com/Facepunch/sbox-walker/blob/main/code/Player/PlayerObserver.cs

/// <summary>
/// Dead players become these. They try to observe their last corpse.
/// </summary>
public sealed class TacitPlayerObserver : Component
{
	[Property,ReadOnly] SceneBoss Boss { get; set; }
	Angles EyeAngles;
	TimeSince timeSinceStarted;

	protected override void OnStart()
	{
		// if(!Boss.IsValid)
		{
			Boss = Scene.Directory.FindByName( "SceneBoss" ).First().GetComponent<SceneBoss>();
		}
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		EyeAngles = Scene.Camera.WorldRotation;
		timeSinceStarted = 0;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		var corpse = Scene.GetAllComponents<TacitPlayerCorpse>()
					.Where( x => x.Connection == Network.Owner )
					.OrderByDescending( x => x.Created )
					.FirstOrDefault();

		if ( corpse.IsValid() )
		{
			RotateAround( corpse );
		}

		// Don't allow immediate respawn
		if ( timeSinceStarted < 1 )
			return;

		// If pressed a button, or has been too long
		if ( Input.Pressed( "attack1" ) || Input.Pressed( "jump" ) || Input.Pressed("use") || timeSinceStarted > 4 )
		{
			Respawn();
			GameObject.Destroy();
		}
	}

	[Rpc.Broadcast( NetFlags.OwnerOnly )]
	public void Respawn()
	{
		if ( !Networking.IsHost ) return;

		//GameManager.Current.SpawnPlayerForConnection( Network.Owner );
		if(Boss.IsValid)
		{
			Boss.SpawnPlayerForConnection( Network.Owner );
		}
		GameObject.Destroy();
	}

	private void RotateAround( TacitPlayerCorpse target )
	{
		// Find the corpse eyes

		if ( !target.Components.Get<SkinnedModelRenderer>().TryGetBoneTransform( "head", out var tx ) )
		{
			tx.Position = target.GameObject.GetBounds().Center;
		}

		var e = EyeAngles;
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp( -90, 90 );
		e.roll = 0.0f;
		EyeAngles = e;

		var center = tx.Position;
		var targetPos = center - EyeAngles.Forward * 150.0f;

		var tr = Scene.Trace.FromTo( center, targetPos ).Radius( 1.0f ).WithoutTags( "ragdoll" ).Run();


		Scene.Camera.WorldPosition = Vector3.Lerp( Scene.Camera.WorldPosition, tr.EndPosition, timeSinceStarted, true );

		Scene.Camera.WorldRotation = EyeAngles;
	}
}
