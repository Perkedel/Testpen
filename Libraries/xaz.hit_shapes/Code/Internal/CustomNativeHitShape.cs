using System;
using Sandbox;

namespace HitShapes;

internal sealed class CustomNativeHitShape : IHitShape
{
    readonly int _slotCount;
    readonly Vector2 _nativeSize;
    readonly Func<Vector2, int?> _resolver;

    public CustomNativeHitShape(int slotCount, Vector2 nativeSize, Func<Vector2, int?> resolver)
    {
        _slotCount = slotCount;
        _nativeSize = nativeSize;
        _resolver = resolver;
    }

    public int SlotCount => _slotCount;

    public int? Resolve(Vector2 local, Vector2 size)
    {
        if (size.x <= 0f || size.y <= 0f) return null;
        var native = new Vector2(local.x * _nativeSize.x / size.x, local.y * _nativeSize.y / size.y);
        var slot = _resolver(native);
        if (slot is int s && (s < 0 || s >= _slotCount))
            throw new InvalidOperationException("HitShape.Custom resolver returned a slot id outside [0, slotCount).");
        return slot;
    }
}
