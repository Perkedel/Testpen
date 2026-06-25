using Sandbox;

namespace HitShapes;

internal sealed class IntersectHitShape : IHitShape
{
    readonly IHitShape _a;
    readonly IHitShape _b;

    public IntersectHitShape(IHitShape a, IHitShape b)
    {
        _a = a;
        _b = b;
    }

    public int SlotCount => _a.SlotCount;

    public int? Resolve(Vector2 local, Vector2 size)
    {
        var sa = _a.Resolve(local, size);
        if (!sa.HasValue) return null;
        if (!_b.Resolve(local, size).HasValue) return null;
        return sa.Value;
    }
}
