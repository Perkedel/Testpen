using Sandbox;

namespace HitShapes;

internal sealed class PolygonHitShape : IHitShape
{
    readonly Vector2[][] _polygons;

    public PolygonHitShape(Vector2[][] polygons)
    {
        _polygons = polygons;
    }

    public int SlotCount => _polygons.Length;

    public int? Resolve(Vector2 local, Vector2 size)
    {
        if (size.x <= 0f || size.y <= 0f) return null;
        var ux = local.x / size.x;
        var uy = local.y / size.y;
        for (int i = 0; i < _polygons.Length; i++)
        {
            if (PointInPolygon(ux, uy, _polygons[i])) return i;
        }
        return null;
    }

    static bool PointInPolygon(float px, float py, Vector2[] verts)
    {
        bool inside = false;
        int n = verts.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var vi = verts[i];
            var vj = verts[j];
            if (((vi.y > py) != (vj.y > py)) &&
                (px < (vj.x - vi.x) * (py - vi.y) / (vj.y - vi.y) + vi.x))
                inside = !inside;
        }
        return inside;
    }
}
