using System;
using Sandbox;

namespace HitShapes;

internal sealed class CustomRawHitShape : IHitShape
{
    readonly int _slotCount;
    readonly Func<Vector2, Vector2, int?> _resolver;

    public CustomRawHitShape(int slotCount, Func<Vector2, Vector2, int?> resolver)
    {
        _slotCount = slotCount;
        _resolver = resolver;
    }

    public int SlotCount => _slotCount;

    public int? Resolve(Vector2 local, Vector2 size)
    {
        var slot = _resolver(local, size);
        if (slot is int s && (s < 0 || s >= _slotCount))
            throw new InvalidOperationException("HitShape.CustomRaw resolver returned a slot id outside [0, slotCount).");
        return slot;
    }
}
