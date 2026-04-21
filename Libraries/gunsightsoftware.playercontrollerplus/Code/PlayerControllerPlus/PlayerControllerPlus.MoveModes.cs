using Sandbox.MovementPlus;

namespace Sandbox;

public sealed partial class PlayerControllerPlus : Component
{
	[FeatureEnabled( "Move Modes", Icon = "directions_run" )]
	public bool UseMoveModes { get; set; } = true;

	// --- Walk ---
	[Property, Feature( "Move Modes" ), Group( "Walk" ), Order( 0 )] public float WalkGroundAngle { get; set; } = 45.0f;
	[Property, Feature( "Move Modes" ), Group( "Walk" ), Order( 1 )] public float WalkStepUpHeight { get; set; } = 18.0f;
	[Property, Feature( "Move Modes" ), Group( "Walk" ), Order( 2 )] public float WalkStepDownHeight { get; set; } = 18.0f;

	// --- Ladder ---
	[Property, Feature( "Move Modes" ), ToggleGroup( "EnableLadderMode", Label = "Enable Ladder" ), Group( "Ladder" ), Order( 100 )] public bool EnableLadderMode { get; set; } = true;
	[Property, Feature( "Move Modes" ), Group( "Ladder" ), Order( 101 )] public int LadderPriority { get; set; } = 5;
	[Property, Feature( "Move Modes" ), Group( "Ladder" ), Order( 102 ), Range( 0, 2 )] public float LadderSpeed { get; set; } = 1;
	[Property, Feature( "Move Modes" ), Group( "Ladder" ), Order( 103 )] public TagSet LadderClimbableTags { get; set; } = new TagSet() { "ladder" };

	// --- Swim ---
	[Property, Feature( "Move Modes" ), ToggleGroup( "EnableSwimMode", Label = "Enable Swim" ), Group( "Swim" ), Order( 200 )] public bool EnableSwimMode { get; set; } = true;
	[Property, Feature( "Move Modes" ), Group( "Swim" ), Order( 201 )] public int SwimPriority { get; set; } = 10;
	[Property, Feature( "Move Modes" ), Group( "Swim" ), Order( 202 ), Range( 0, 1 )] public float SwimLevel { get; set; } = 0.7f;

	// --- Sit ---
	[Property, Feature( "Move Modes" ), ToggleGroup( "EnableSitMode", Label = "Enable Sit" ), Group( "Sit" ), Order( 300 )] public bool EnableSitMode { get; set; } = true;

	// --- Noclip ---
	[Property, Feature( "Move Modes" ), ToggleGroup( "EnableNoclipMode", Label = "Enable Noclip" ), Group( "Noclip" ), Order( 400 )] public bool EnableNoclipMode { get; set; }
	[Property, Feature( "Move Modes" ), Group( "Noclip" ), Order( 401 )] public bool NoclipActive { get; set; }
	[Property, Feature( "Move Modes" ), Group( "Noclip" ), Order( 402 )] public bool NoclipToggleWithAirJump { get; set; }
	[Property, Feature( "Move Modes" ), Group( "Noclip" ), Order( 403 )] public bool NoclipEnableCollision { get; set; }
	[Property, Feature( "Move Modes" ), Group( "Noclip" ), Order( 404 )] public float NoclipRunSpeed { get; set; } = 1200;
	[Property, Feature( "Move Modes" ), Group( "Noclip" ), Order( 405 )] public float NoclipWalkSpeed { get; set; } = 200;

	// --- Mode Instances ---
	internal MoveModeWalkPlus _walkMode;
	internal MoveModeSwimPlus _swimMode;
	internal MoveModeLadderPlus _ladderMode;
	internal SitMoveModePlus _sitMode;
	internal NoclipMoveModePlus _noclipMode;

	void CreateModeInstances()
	{
		_walkMode = new MoveModeWalkPlus { Controller = this };
		_swimMode = new MoveModeSwimPlus { Controller = this };
		_ladderMode = new MoveModeLadderPlus { Controller = this };
		_sitMode = new SitMoveModePlus { Controller = this };
		_noclipMode = new NoclipMoveModePlus { Controller = this };
	}

	MoveModePlus[] GetAllModes()
	{
		return new MoveModePlus[] { _walkMode, _swimMode, _ladderMode, _sitMode, _noclipMode };
	}

	bool IsModeEnabled( MoveModePlus mode )
	{
		if ( mode == _walkMode ) return true;
		if ( mode == _swimMode ) return EnableSwimMode;
		if ( mode == _ladderMode ) return EnableLadderMode;
		if ( mode == _sitMode ) return EnableSitMode;
		if ( mode == _noclipMode ) return EnableNoclipMode;
		if ( mode is ExternalMoveModeAdapter ) return true;
		return false;
	}

	void UpdateModeFixedUpdates()
	{
		if ( EnableSwimMode ) _swimMode.FixedUpdate();
		if ( EnableLadderMode ) _ladderMode.FixedUpdate();
		if ( EnableNoclipMode ) _noclipMode.FixedUpdate();
	}

	void OnFailPressingSit()
	{
		if ( EnableSitMode && Mode == _sitMode )
		{
			_sitMode.OnFailPressing();
		}
	}
}
