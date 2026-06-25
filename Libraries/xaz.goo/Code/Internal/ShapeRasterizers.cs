using System;
using Sandbox;

namespace Goo.Internal;

internal static class ShapeRasterizers
{
    public const int BakeResolution = 512;

    // Even-odd scanline fill of unit-coord [0..1] points onto the BakeResolution square; edges use a 16-sub-row supersample. Fewer than 3 points bakes a transparent mask.
    public static byte[] BakePolygon(Vector2[] points)
    {
        int W = BakeResolution;
        byte[] px = new byte[W * W * 4];

        // Pre-fill RGB to white; alpha defaults to 0 (transparent).
        for (int i = 0; i < px.Length; i += 4)
        {
            px[i + 0] = 255;
            px[i + 1] = 255;
            px[i + 2] = 255;
        }

        if (points is null || points.Length < 3) return px;

        const int SubSamples = 16;
        Span<float> xs = stackalloc float[64];
        Span<int> cover = stackalloc int[W];

        for (int y = 0; y < W; y++)
        {
            // Coverage = fraction of sub-rows where this pixel lies inside the polygon.
            // Accumulated per pixel column across sub-rows; final alpha = accum / SubSamples.
            cover.Clear();

            for (int s = 0; s < SubSamples; s++)
            {
                float yLine = y + (s + 0.5f) / SubSamples;
                int count = 0;

                for (int i = 0; i < points.Length; i++)
                {
                    var p1 = points[i];
                    var p2 = points[(i + 1) % points.Length];
                    float y1 = p1.y * W;
                    float y2 = p2.y * W;
                    if ((y1 <= yLine && y2 > yLine) || (y2 <= yLine && y1 > yLine))
                    {
                        float t = (yLine - y1) / (y2 - y1);
                        float xCross = (p1.x + t * (p2.x - p1.x)) * W;
                        if (count < xs.Length) xs[count++] = xCross;
                    }
                }

                if (count < 2) continue;

                // Insertion sort (n is small, typically 2-8).
                for (int i = 1; i < count; i++)
                {
                    float key = xs[i];
                    int j = i - 1;
                    while (j >= 0 && xs[j] > key) { xs[j + 1] = xs[j]; j--; }
                    xs[j + 1] = key;
                }

                for (int i = 0; i + 1 < count; i += 2)
                {
                    int xStart = (int)MathF.Ceiling(xs[i]);
                    int xEnd   = (int)MathF.Floor(xs[i + 1]);
                    if (xStart < 0) xStart = 0;
                    if (xEnd >= W) xEnd = W - 1;
                    for (int x = xStart; x <= xEnd; x++) cover[x]++;
                }
            }

            for (int x = 0; x < W; x++)
            {
                if (cover[x] == 0) continue;
                int idx = (y * W + x) * 4 + 3;
                int a = cover[x] * 255 / SubSamples;
                if (a > 255) a = 255;
                px[idx] = (byte)a;
            }
        }

        return px;
    }

    public static byte[] BakeSector(in ShapeParams p)
    {
        int W = BakeResolution;
        byte[] px = new byte[W * W * 4];
        float center = W * 0.5f;
        float halfDim = center;
        float outerR = halfDim * p.D;       // OuterRadius fraction
        float innerR = halfDim * p.C;       // InnerRadius fraction
        float cornerPx = halfDim * p.E;     // CornerRadius fraction of outer radius (in bake-resolution pixels)
        const float AaBand = 1.0f;

        // Angle convention: 0 deg = up (12 o'clock), clockwise positive.
        // In screen coords (y-down), MathF.Atan2(dx, -dy) puts 0 at up, cw+.
        float startRad = p.A * MathF.PI / 180f;
        float endRad   = p.B * MathF.PI / 180f;
        float arcRad   = endRad - startRad;
        if (arcRad < 0) arcRad += MathF.Tau;
        if (arcRad > MathF.Tau) arcRad = MathF.Tau;
        bool fullSweep = arcRad >= MathF.Tau - 1e-4f;
        bool roundCorners = cornerPx > 0f && !fullSweep;

        for (int y = 0; y < W; y++)
        for (int x = 0; x < W; x++)
        {
            float dx = x - center, dy = y - center;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            int idx = (y * W + x) * 4;
            px[idx + 0] = 255;
            px[idx + 1] = 255;
            px[idx + 2] = 255;

            // Radial band with 1-px SmoothStep AA
            float outerA = 1f - SmoothStep(outerR - AaBand, outerR + AaBand, dist);
            float innerA = SmoothStep(innerR - AaBand, innerR + AaBand, dist);
            float a = outerA * innerA;

            if (!fullSweep && a > 0f)
            {
                // 0=up, cw+: atan2(dx, -dy) maps right (dx>0, dy=0) to +pi/2
                float angle = MathF.Atan2(dx, -dy);                  // -pi..pi
                if (angle < 0) angle += MathF.Tau;                   // 0..2pi
                float rel = angle - startRad;
                while (rel < 0)          rel += MathF.Tau;
                while (rel >= MathF.Tau) rel -= MathF.Tau;
                float angAaRad = AaBand / MathF.Max(dist, 1f);
                float angA = SmoothStep(-angAaRad, angAaRad, rel);
                float angB = 1f - SmoothStep(arcRad - angAaRad, arcRad + angAaRad, rel);
                a *= angA * angB;

                if (roundCorners && a > 0f)
                {
                    // Signed distances from the pixel to each of the 4 wedge edges.
                    // Positive = inside the wedge from that edge.
                    float dInner   = dist - innerR;                  // distance past inner arc
                    float dOuter   = outerR - dist;                  // distance before outer arc
                    float dStart   = rel * dist;                     // arc-length from start radial
                    float dEnd     = (arcRad - rel) * dist;          // arc-length to end radial

                    // Corner regions are disjoint for small E; MathF.Min guards against overlap.
                    float cornerAlpha = 1f;
                    if (dInner < cornerPx && dStart < cornerPx)
                    {
                        float cx = cornerPx - dInner;
                        float cy = cornerPx - dStart;
                        float d  = MathF.Sqrt(cx * cx + cy * cy);
                        cornerAlpha = MathF.Min(cornerAlpha, 1f - SmoothStep(cornerPx - AaBand, cornerPx + AaBand, d));
                    }
                    if (dInner < cornerPx && dEnd < cornerPx)
                    {
                        float cx = cornerPx - dInner;
                        float cy = cornerPx - dEnd;
                        float d  = MathF.Sqrt(cx * cx + cy * cy);
                        cornerAlpha = MathF.Min(cornerAlpha, 1f - SmoothStep(cornerPx - AaBand, cornerPx + AaBand, d));
                    }
                    if (dOuter < cornerPx && dStart < cornerPx)
                    {
                        float cx = cornerPx - dOuter;
                        float cy = cornerPx - dStart;
                        float d  = MathF.Sqrt(cx * cx + cy * cy);
                        cornerAlpha = MathF.Min(cornerAlpha, 1f - SmoothStep(cornerPx - AaBand, cornerPx + AaBand, d));
                    }
                    if (dOuter < cornerPx && dEnd < cornerPx)
                    {
                        float cx = cornerPx - dOuter;
                        float cy = cornerPx - dEnd;
                        float d  = MathF.Sqrt(cx * cx + cy * cy);
                        cornerAlpha = MathF.Min(cornerAlpha, 1f - SmoothStep(cornerPx - AaBand, cornerPx + AaBand, d));
                    }
                    a *= cornerAlpha;
                }
            }

            a = MathF.Max(0f, MathF.Min(1f, a));
            px[idx + 3] = (byte)(a * 255f);
        }
        return px;
    }

    public static byte[] BakeArc(in ShapeParams p)
    {
        // Arc = sector with inner = mid - stroke/2, outer = mid + stroke/2.
        float halfStroke = p.D * 0.5f;
        var sectorParams = new ShapeParams
        {
            A = p.A,
            B = p.B,
            C = MathF.Max(0f, p.C - halfStroke),
            D = MathF.Min(1f, p.C + halfStroke),
        };
        return BakeSector(in sectorParams);
    }

    static float SmoothStep(float edge0, float edge1, float x)
    {
        if (edge0 == edge1) return x < edge0 ? 0f : 1f;
        float t = MathF.Max(0f, MathF.Min(1f, (x - edge0) / (edge1 - edge0)));
        return t * t * (3f - 2f * t);
    }
}
