using System;
using Sandbox;
using Sandbox.UI;

namespace Goo;

/// <summary>Factories for composite shape subtrees. Rotation-applying helpers default <c>PointerEvents.None</c> so rotated descendants do not drift parent hit dispatch.</summary>
public static class Shapes
{
    /// <summary>N <see cref="Sector"/> wedges clockwise from 12 o'clock; the subtree is <c>PointerEvents.None</c> so parent dispatchers see the unrotated frame.</summary>
    public static Container Ring(int segments, float innerRadius, ReadOnlySpan<Color> colors,
        float outerRadius = 1.0f, string? key = null)
    {
        if (segments <= 0)
            throw new ArgumentException("segments must be > 0", nameof(segments));
        if (colors.Length != segments)
            throw new ArgumentException($"colors.Length ({colors.Length}) must equal segments ({segments})", nameof(colors));

        var outer = new Container
        {
            Key           = key,
            Position      = PositionMode.Relative,
            Width         = Length.Percent(100),
            Height        = Length.Percent(100),
            PointerEvents = PointerEvents.None,
        };

        float wedge = 360f / segments;
        float halfWedge = 0.5f * wedge;
        for (int i = 0; i < segments; i++)
        {
            float rot = i * wedge - halfWedge;
            var wrapper = new Container
            {
                Key           = $"ring-slot-{i}",
                Position      = PositionMode.Absolute,
                Top           = 0,
                Left          = 0,
                Width         = Length.Percent(100),
                Height        = Length.Percent(100),
                PointerEvents = PointerEvents.None,
                Transform     = Goo.PanelTransform.Rotate(rot),
            };
            wrapper.Children.Add(new Sector
            {
                Width           = Length.Percent(100),
                Height          = Length.Percent(100),
                StartAngle      = 0f,
                EndAngle        = wedge,
                InnerRadius     = innerRadius,
                OuterRadius     = outerRadius,
                BackgroundColor = colors[i],
            });
            outer.Children.Add(wrapper);
        }
        return outer;
    }

    /// <summary>Color-array overload; forwards to the <see cref="ReadOnlySpan{T}"/> form. Prefer the span overload from animated <c>Build()</c> bodies to avoid per-frame allocation.</summary>
    public static Container Ring(int segments, float innerRadius, Color[] colors,
        float outerRadius = 1.0f, string? key = null)
    {
        if (colors is null)
            throw new ArgumentException("colors must not be null", nameof(colors));
        return Ring(segments, innerRadius, (ReadOnlySpan<Color>)colors, outerRadius, key);
    }

    /// <summary>A pre-rounded circular <see cref="Container"/> (50% radius, 100% size) that drops into a flex slot.</summary>
    public static Container Disc(string? key = null)
        => new Container
        {
            Key          = key,
            Width        = Length.Percent(100),
            Height       = Length.Percent(100),
            BorderRadius = Length.Percent(50),
        };
}
