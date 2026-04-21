using System;
using System.Collections.Generic;
using Sandbox;

/// <summary>
/// A general purpose prop  grabber.
/// Works with only a transform for the raycast (like a camera). Optionally interacts with an IPropUser for custom behavior.
/// </summary>
public sealed class PropGrabber : Component
{
	/// <summary>
	/// The GameObject to use as the transform origin of the raycast for picking up props (e.g. a camera).
	/// </summary>
	[Property] public GameObject RaycastSource { get; set; }

	/// <summary>
	/// How far the grabber can pick up props from, in units.
	/// </summary>
	[Property, Group( "Settings" )] public float PickupRange { get; set; } = 200f;
	/// <summary>
	/// The distance from the RaycastSource where the prop is held.
	/// </summary>
	[Property, Group( "Settings" )] public float HoldDistance { get; set; } = 70f;
	/// <summary>
	/// The force applied to the prop when thrown.
	/// </summary>
	[Property, Group( "Settings" )] public float ThrowForce { get; set; } = 300f;
	/// <summary>
	/// The max velocity allowed before the prop is automatically dropped.
	/// Only matters if <see cref="DropFromVelocity"/> is true.
	/// </summary>
	[Property, Group( "Settings" )] public float MaxHoldVelocity { get; set; } = 2000f;
	/// <summary>
	/// The max angle (degrees) from the forward direction before the prop is dropped from being out of view.
	/// Only matters if <see cref="DropFromAngle"/> is true.
	/// </summary>
	[Property, Group( "Settings" )] public float MaxFovAngle { get; set; } = 60f;
	/// <summary>
	/// Increase to make the pop move faster. Decrease for smoother movement, but too low makes it move stubbornly.
	/// </summary>
	[Property, Group( "Settings" )] public float MaxMoveSpeed { get; set; } = 500f;

	/// <summary>
	/// Whether the player is allowed to throw the held prop.
	/// </summary>
	[Property, Group( "Toggles" )] public bool CanThrow { get; set; } = true;
	/// <summary>
	/// Whether the prop should be dropped if its velocity exceeds <see cref="MaxHoldVelocity"/>.
	/// </summary>
	[Property, Group( "Toggles" )] public bool DropFromVelocity { get; set; } = true;
	/// <summary>
	/// Whether the prop should be dropped if it moves outside the field of view defined by <see cref="MaxFovAngle"/>.
	/// </summary>
	[Property, Group( "Toggles" )] public bool DropFromAngle { get; set; } = true;

	/// <summary>
	/// Limits pickup to objects with a <see cref="Prop"/> component.
	/// </summary>
	[Property, Group( "Pickup Requirements" )]
	public bool RequirePropComponent = true;

	/// <summary>
	/// Tags that the pickupable object must have in order to be picked up, like "prop".
	/// Leave empty to ignore requiring any tag.
	/// </summary>
	[Property, Group( "Pickup Requirements" )]
	public TagSet RequireTags { get; set; } = new();

	[Sync] private Rigidbody HeldRigidbody { get; set; }
	[Sync] private Vector3 TargetPosition { get; set; }

	[Property]
	private IPropUser PropUser;

	protected override void OnUpdate()
	{
		if ( !RaycastValid() )
		{
			Log.Warning("No RaycastSource found for PropGrabber.");
			return;
		}

		HandleInput();
		UpdateHeldPropPosition();
		CheckDropConditions();
	}

	private bool RaycastValid() => RaycastSource != null && RaycastSource.IsValid();

	private void HandleInput()
	{
		if ( Network.IsProxy ) return;

		if ( Input.Pressed( "use" ) )
		{
			if ( HeldRigidbody == null )
				TryPickup();
			else
				DropProp();
		}
		else if ( Input.Pressed( "attack1" ) && HeldRigidbody != null && CanThrow )
		{
			ThrowProp();
		}
	}

	private void TryPickup()
	{
		var ray = new Ray( RaycastSource.WorldPosition, RaycastSource.WorldRotation.Forward );

		var trace = Scene.Trace
			.Ray( ray, PickupRange )
			.WithAnyTags( RequireTags )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !trace.Hit || trace.GameObject == null ) return;

		Rigidbody rigidbody = trace.GameObject.Components.Get<Rigidbody>();
		if ( rigidbody == null ) return;

		if ( RequirePropComponent )
		{
			Prop prop = trace.GameObject.Components.Get<Prop>();
			if ( prop == null || !prop.IsValid() )
				return;
		}

		HeldRigidbody = rigidbody;

		if ( !trace.GameObject.Network.IsOwner )
			trace.GameObject.Network.TakeOwnership();

		HeldRigidbody.Gravity = false;
		PropUser?.OnPropGrabbed();
	}

	private void UpdateHeldPropPosition()
	{
		if ( HeldRigidbody == null ) return;

		Vector3 sourcePos = RaycastSource.WorldPosition;
		Rotation sourceRot = RaycastSource.WorldRotation;
		TargetPosition = sourcePos + sourceRot.Forward * HoldDistance;

		Vector3 desiredVelocity = (TargetPosition - HeldRigidbody.WorldPosition) * 20f;

		if ( desiredVelocity.Length > MaxMoveSpeed )
		{
			Vector3 direction = desiredVelocity.Normal;
			desiredVelocity = direction * MaxMoveSpeed;
		}

		HeldRigidbody.Velocity = desiredVelocity;
		HeldRigidbody.AngularVelocity *= 0.9f;
	}

	private void CheckDropConditions()
	{
		if ( HeldRigidbody == null ) return;

		if ( DropFromVelocity && HeldRigidbody.Velocity.Length > MaxHoldVelocity )
		{
			DropProp();
			return;
		}

		Vector3 propDirection = (HeldRigidbody.WorldPosition - RaycastSource.WorldPosition).Normal;
		Vector3 sourceForward = RaycastSource.WorldRotation.Forward;
		float dot = Vector3.Dot( sourceForward, propDirection );
		float angle = MathF.Acos( dot ) * 180f / MathF.PI;

		if ( DropFromAngle && angle > MaxFovAngle )
		{
			DropProp();
		}
	}

	private void DropProp()
	{
		if ( HeldRigidbody == null ) return;

		HeldRigidbody.Gravity = true;
		HeldRigidbody.AngularVelocity = Vector3.Zero;

		PropUser?.OnPropReleased();
		CleanupHeldProp();
	}

	private void ThrowProp()
	{
		if ( HeldRigidbody == null ) return;

		HeldRigidbody.Gravity = true;
		HeldRigidbody.Velocity = RaycastSource.WorldRotation.Forward * ThrowForce;

		PropUser?.OnPropReleased();
		CleanupHeldProp();
	}

	private void CleanupHeldProp()
	{
		if ( HeldRigidbody != null && HeldRigidbody.IsValid() && HeldRigidbody.GameObject.Network.IsOwner )
		{
			HeldRigidbody.GameObject.Network.DropOwnership();
		}

		HeldRigidbody = null;
	}

	protected override void OnDestroy()
	{
		DropProp();
	}
}
