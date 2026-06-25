using System.Collections.Concurrent;

namespace HitShapes;

internal static class ShapeCache
{
    static readonly ConcurrentDictionary<(int slots, float innerRatio, float outerRatio, float startAngleDeg), IHitShape> _radial = new();
    static readonly ConcurrentDictionary<(int cols, int rows), IHitShape> _rectGrid = new();

    public static IHitShape GetOrCreateRadial(int slots, float innerRatio, float outerRatio, float startAngleDeg)
        => _radial.GetOrAdd((slots, innerRatio, outerRatio, startAngleDeg),
            k => new RadialHitShape(k.slots, k.innerRatio, k.outerRatio, k.startAngleDeg));

    public static IHitShape GetOrCreateRectGrid(int cols, int rows)
        => _rectGrid.GetOrAdd((cols, rows),
            k => new RectGridHitShape(k.cols, k.rows));
}
