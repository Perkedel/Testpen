#nullable enable
using HitShapes;
using Sandbox;
using Xunit;

namespace HitShapes.Tests;

public class ShapeCacheTests
{
    [Fact]
    public void Radial_same_params_returns_same_instance()
    {
        var a = HitShape.Radial(slots: 8, innerRatio: 0.4f);
        var b = HitShape.Radial(slots: 8, innerRatio: 0.4f);
        Assert.Same(a, b);
    }

    [Fact]
    public void Radial_default_outer_ratio_participates_in_cache_key()
    {
        var a = HitShape.Radial(slots: 8, innerRatio: 0.4f);
        var b = HitShape.Radial(slots: 8, innerRatio: 0.4f, outerRatio: 1f);
        Assert.Same(a, b);
    }

    [Fact]
    public void Radial_different_params_return_different_instances()
    {
        var a = HitShape.Radial(slots: 8);
        var b = HitShape.Radial(slots: 9);
        Assert.NotSame(a, b);
    }

    [Fact]
    public void RectGrid_same_params_returns_same_instance()
    {
        var a = HitShape.RectGrid(cols: 4, rows: 3);
        var b = HitShape.RectGrid(cols: 4, rows: 3);
        Assert.Same(a, b);
    }

    [Fact]
    public void CustomRaw_not_cached_each_call_returns_new_instance()
    {
        var a = HitShape.CustomRaw(slotCount: 1, (_, _) => null);
        var b = HitShape.CustomRaw(slotCount: 1, (_, _) => null);
        Assert.NotSame(a, b);
    }

    [Fact]
    public void Polygon_not_cached_each_call_returns_new_instance()
    {
        Vector2[] verts = { new(0f, 0f), new(1f, 0f), new(0.5f, 1f) };
        var a = HitShape.Polygon(verts);
        var b = HitShape.Polygon(verts);
        Assert.NotSame(a, b);
    }

    [Fact]
    public void Polygons_not_cached_each_call_returns_new_instance()
    {
        Vector2[][] polys = {
            new Vector2[] { new(0f, 0f), new(1f, 0f), new(0.5f, 1f) },
            new Vector2[] { new(0f, 0f), new(1f, 0f), new(0.5f, 1f) },
        };
        var a = HitShape.Polygons(polys);
        var b = HitShape.Polygons(polys);
        Assert.NotSame(a, b);
    }

    [Fact]
    public void Union_not_cached_each_call_returns_new_instance()
    {
        var a = HitShape.Radial(4);
        var b = HitShape.RectGrid(2, 2);
        Assert.NotSame(HitShape.Union(a, b), HitShape.Union(a, b));
    }

    [Fact]
    public void Intersect_not_cached_each_call_returns_new_instance()
    {
        var a = HitShape.Radial(4);
        var b = HitShape.RectGrid(2, 2);
        Assert.NotSame(HitShape.Intersect(a, b), HitShape.Intersect(a, b));
    }

    [Fact]
    public void Difference_not_cached_each_call_returns_new_instance()
    {
        var a = HitShape.Radial(4);
        var b = HitShape.RectGrid(2, 2);
        Assert.NotSame(HitShape.Difference(a, b), HitShape.Difference(a, b));
    }
}
