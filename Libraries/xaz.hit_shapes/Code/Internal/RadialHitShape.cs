using System;
using Sandbox;

namespace HitShapes;

internal sealed class RadialHitShape : IHitShape, IGeometricHitShape
{
    readonly int _slots;
    readonly float _innerRatio;
    readonly float _outerRatio;
    readonly float _startRad;       // start offset in radians (0 = baseline / slot 0 at 12 o'clock)
    readonly float _wedgeSize;
    readonly float _halfWedge;

    public RadialHitShape(int slots, float innerRatio, float outerRatio, float startAngleDeg = 0f)
    {
        _slots = slots;
        _innerRatio = innerRatio;
        _outerRatio = outerRatio;
        _startRad = startAngleDeg * MathF.PI / 180f;
        _wedgeSize = MathF.Tau / slots;
        _halfWedge = _wedgeSize * 0.5f;
    }

    public int SlotCount => _slots;

    public int? Resolve(Vector2 local, Vector2 size)
    {
        return ResolveWithGeometry(local, size, out _, out _);
    }

    public int? ResolveWithGeometry(Vector2 local, Vector2 size, out float? angleDeg, out float? distanceNormalized)
    {
        var dx = local.x - size.x * 0.5f;
        var dy = local.y - size.y * 0.5f;
        var dist2 = dx * dx + dy * dy;

        var radius = MathF.Min(size.x, size.y) * 0.5f;
        var outerR = radius * _outerRatio;
        var innerR = radius * _innerRatio;
        if (dist2 < innerR * innerR || dist2 > outerR * outerR)
        {
            angleDeg = null;
            distanceNormalized = null;
            return null;
        }

        // Angle shifted by _startRad and half-wedge so slot 0 is centered on the start direction.
        var angle = MathF.Atan2(dy, dx) + MathF.PI * 0.5f + _halfWedge - _startRad;
        while (angle < 0) angle += MathF.Tau;
        while (angle >= MathF.Tau) angle -= MathF.Tau;

        // rawAngle is in the wheel's absolute frame; only slot math shifts by _startRad.
        var rawAngle = MathF.Atan2(dy, dx) + MathF.PI * 0.5f;
        if (rawAngle < 0) rawAngle += MathF.Tau;
        if (rawAngle >= MathF.Tau) rawAngle -= MathF.Tau;
        angleDeg = rawAngle * (180f / MathF.PI);
        distanceNormalized = radius > 0 ? MathF.Sqrt(dist2) / radius : 0f;
        return (int)(angle / _wedgeSize) % _slots;
    }
}
