using Sandbox;

namespace HitShapes;

internal sealed class UnionHitShape : IHitShape
{
    readonly IHitShape _a;
    readonly IHitShape _b;

    public UnionHitShape(IHitShape a, IHitShape b)
    {
        _a = a;
        _b = b;
    }

    public int SlotCount => _a.SlotCount + _b.SlotCount;

    public int? Resolve(Vector2 local, Vector2 size)
    {
        if (_a.Resolve(local, size) is int sa) return sa;
        if (_b.Resolve(local, size) is int sb) return sb + _a.SlotCount;
        return null;
    }
}
