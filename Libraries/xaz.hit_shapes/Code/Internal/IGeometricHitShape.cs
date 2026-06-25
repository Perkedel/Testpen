using Sandbox;

namespace HitShapes;

/// <summary>
/// Optional companion to <see cref="IHitShape"/> for shapes that carry geometric
/// state worth exposing to consumers (e.g. cursor angle on a radial). Resolves
/// to the same slot id as <see cref="IHitShape.Resolve"/> and additionally
/// reports the resolved angle/distance when applicable. Implementations are
/// stateless; outputs depend only on the call arguments.
/// </summary>
internal interface IGeometricHitShape
{
    /// <summary>
    /// Resolve a slot and report companion geometry. <paramref name="angleDeg"/>
    /// follows the convention 0 = up, clockwise positive, [0, 360).
    /// <paramref name="distanceNormalized"/> is 0..1 from center to outer radius.
    /// Both are <c>null</c> when the position falls outside the shape.
    /// </summary>
    int? ResolveWithGeometry(Vector2 localPosition, Vector2 panelSize, out float? angleDeg, out float? distanceNormalized);
}
