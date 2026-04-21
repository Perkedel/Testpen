using Sandbox.MovementPlus;
using System.Collections.Generic;

namespace Sandbox;


public sealed partial class PlayerControllerPlus : Component
{
	public MoveModePlus Mode { get; private set; }

	/// <summary>
	/// The currently active ExternalMoveMode component, or null if a built-in mode is active.
	/// </summary>
	public ExternalMoveMode ActiveExternalMode
	{
		get
		{
			if ( Mode is ExternalMoveModeAdapter adapter && adapter.External.IsValid() )
				return adapter.External;
			return null;
		}
	}

	private Dictionary<ExternalMoveMode, ExternalMoveModeAdapter> _externalAdapters = new();

	ExternalMoveModeAdapter GetOrCreateAdapter( ExternalMoveMode ext )
	{
		if ( _externalAdapters.TryGetValue( ext, out var adapter ) )
			return adapter;

		adapter = new ExternalMoveModeAdapter { Controller = this, External = ext };
		ext.Controller = this;
		_externalAdapters[ext] = adapter;
		return adapter;
	}

	void ChooseBestMoveMode()
	{
		MoveModePlus best = null;
		int bestScore = int.MinValue;

		foreach ( var mode in GetAllModes() )
		{
			if ( !IsModeEnabled( mode ) ) continue;

			var score = mode.Score( this );
			if ( score > bestScore )
			{
				bestScore = score;
				best = mode;
			}
		}

		foreach ( var ext in GetComponents<ExternalMoveMode>() )
		{
			if ( !ext.Enabled ) continue;

			var score = ext.Score( this );
			if ( score > bestScore )
			{
				bestScore = score;
				best = GetOrCreateAdapter( ext );
			}
		}

		if ( best == null ) best = _walkMode;
		if ( Mode == best ) return;

		Mode?.OnModeEnd( best );

		Mode = best;

		if ( Body?.PhysicsBody is { } body )
		{
			body.Sleeping = false;
		}

		Mode?.OnModeBegin();
	}
}
