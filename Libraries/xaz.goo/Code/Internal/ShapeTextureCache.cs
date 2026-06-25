using System;
using System.Collections.Generic;
using Sandbox;

namespace Goo.Internal;

// Variable-length cache key for Polygon. Holds the same Vector2[] reference the user
// passed to the Blob (no defensive copy); structural equality + content hash let two
// independently-constructed arrays with identical contents share a cache slot.
internal readonly struct PolygonKey : IEquatable<PolygonKey>
{
    public readonly Vector2[] Points;
    public PolygonKey(Vector2[] points) { Points = points; }

    public bool Equals(PolygonKey other)
    {
        if (ReferenceEquals(Points, other.Points)) return true;
        if (Points is null || other.Points is null) return false;
        if (Points.Length != other.Points.Length) return false;
        for (int i = 0; i < Points.Length; i++)
            if (Points[i] != other.Points[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is PolygonKey k && Equals(k);

    public override int GetHashCode()
    {
        if (Points is null) return 0;
        var h = new HashCode();
        h.Add(Points.Length);
        for (int i = 0; i < Points.Length; i++)
        {
            h.Add(Points[i].x);
            h.Add(Points[i].y);
        }
        return h.ToHashCode();
    }
}

internal static class ShapeTextureCache
{
    // Key is (kind, ShapeParams) so ShapeParams.Equals/GetHashCode (all 5 fields) govern lookup.
    static readonly Dictionary<(BlobKind, ShapeParams), Texture?> _cache = new();
    static readonly Dictionary<PolygonKey, Texture?> _polygonCache = new();

    // Test seam: when non-null, replaces the real Texture.Create path. The
    // production path is exercised at runtime; tests inject a counter here.
    // Assigned only from Code.Tests; suppress CS0649 in the production build.
#pragma warning disable CS0649
    internal static Func<BlobKind, ShapeParams, Texture?>? TestBaker;
    internal static Func<PolygonKey, Texture?>? TestPolygonBaker;
#pragma warning restore CS0649

    public static Texture? GetOrBake(BlobKind kind, in ShapeParams p)
    {
        var key = (kind, p);
        if (_cache.TryGetValue(key, out var tex)) return tex;

        if (TestBaker is not null)
        {
            tex = TestBaker(kind, p);
        }
        else
        {
            byte[] rgba = kind switch
            {
                BlobKind.Sector => ShapeRasterizers.BakeSector(in p),
                BlobKind.Arc    => ShapeRasterizers.BakeArc(in p),
                _ => throw new InvalidOperationException($"ShapeTextureCache: unsupported BlobKind {kind}"),
            };
            int W = ShapeRasterizers.BakeResolution;
            // PackKey still useful as a debug-name suffix (stable hash); ignores E
            // which is fine for a name.
            long nameKey = p.PackKey(kind);
            tex = Texture.Create(W, W).WithName($"goo-shape-{kind}-{nameKey:x}").WithData(rgba).Finish();
        }
        _cache[key] = tex;
        return tex;
    }

    public static Texture? GetOrBakePolygon(Vector2[] points)
    {
        var key = new PolygonKey(points);
        if (_polygonCache.TryGetValue(key, out var tex)) return tex;

        if (TestPolygonBaker is not null)
        {
            tex = TestPolygonBaker(key);
        }
        else
        {
            byte[] rgba = ShapeRasterizers.BakePolygon(points);
            int W = ShapeRasterizers.BakeResolution;
            tex = Texture.Create(W, W).WithName($"goo-shape-polygon-{key.GetHashCode():x}").WithData(rgba).Finish();
        }
        _polygonCache[key] = tex;
        return tex;
    }

    // Test seam: clears the cache between tests.
    internal static void Reset()
    {
        _cache.Clear();
        _polygonCache.Clear();
    }
}
