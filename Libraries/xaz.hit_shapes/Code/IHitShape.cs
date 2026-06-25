using Sandbox;

namespace HitShapes;

/// <summary>Maps a panel-local cursor position to a slot id, or null if outside the shape.</summary>
public interface IHitShape
{
    /// <summary>Total number of slots (slot ids: 0..SlotCount-1).</summary>
    int SlotCount { get; }

    /// <summary>Map a panel-local cursor position to a slot id, or null if outside the shape.</summary>
    int? Resolve(Vector2 localPosition, Vector2 panelSize);
}
