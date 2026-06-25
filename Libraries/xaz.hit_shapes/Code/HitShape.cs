using System;
using Sandbox;

namespace HitShapes;

/// <summary>Factory entry points for built-in <see cref="IHitShape"/> implementations.</summary>
public static class HitShape
{
    public static IHitShape Radial(int slots, float innerRatio = 0f, float outerRatio = 1f, float startAngleDeg = 0f)
        => ShapeCache.GetOrCreateRadial(slots, innerRatio, outerRatio, startAngleDeg);

    public static IHitShape RectGrid(int cols, int rows)
        => ShapeCache.GetOrCreateRectGrid(cols, rows);

    public static IHitShape Custom(int slotCount, Vector2 nativeSize, Func<Vector2, int?> resolver)
        => new CustomNativeHitShape(slotCount, nativeSize, resolver ?? throw new ArgumentNullException(nameof(resolver)));

    public static IHitShape CustomRaw(int slotCount, Func<Vector2, Vector2, int?> resolver)
        => new CustomRawHitShape(slotCount, resolver ?? throw new ArgumentNullException(nameof(resolver)));

    public static IHitShape Polygon(params Vector2[] verts)
    {
        if (verts == null) throw new ArgumentNullException(nameof(verts));
        if (verts.Length < 3) throw new ArgumentException("Polygon requires at least 3 vertices.");
        return new PolygonHitShape(new[] { verts });
    }

    public static IHitShape Polygons(params Vector2[][] polys)
    {
        if (polys == null) throw new ArgumentNullException(nameof(polys));
        for (int i = 0; i < polys.Length; i++)
        {
            if (polys[i] == null) throw new ArgumentNullException(nameof(polys));
            if (polys[i].Length < 3) throw new ArgumentException("Polygon requires at least 3 vertices.");
        }
        return new PolygonHitShape(polys);
    }

    public static IHitShape Union(IHitShape a, IHitShape b)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        return new UnionHitShape(a, b);
    }

    public static IHitShape Intersect(IHitShape a, IHitShape b)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        return new IntersectHitShape(a, b);
    }

    public static IHitShape Difference(IHitShape a, IHitShape b)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        return new DifferenceHitShape(a, b);
    }
}
