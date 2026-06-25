using System;
using Sandbox;
using Sandbox.UI;

namespace Goo;

/// <summary>
/// Factories for skin (border-image) styling on a <see cref="Container"/>.
/// </summary>
public static class Skins
{
    /// <summary>Dev-diagnostic sink for the zero-width-border nine-slice case; null by default so non-logging contexts (tests) can use the helper.</summary>
    public static Action<string>? OnZeroBorder;

    /// <summary>A nine-slice skinned <see cref="Container"/> that sets source, slice insets, and rendered border together so the slice cannot be set without a width. <paramref name="sliceSrcPx"/> is in source-image pixels; <paramref name="renderedBorder"/> is on-screen px.</summary>
    public static Container NineSlice(
        Texture? source,
        float sliceSrcPx,
        float renderedBorder,
        BorderImageRepeat repeat = BorderImageRepeat.Stretch,
        BorderImageFill fill = BorderImageFill.Filled,
        string? key = null)
    {
        if (sliceSrcPx <= 0f)
            throw new ArgumentException("sliceSrcPx must be > 0 (the corner-art inset in source pixels)", nameof(sliceSrcPx));

        if (ShouldWarnZeroBorder(source != null, renderedBorder))
            OnZeroBorder?.Invoke($"Goo.Skins.NineSlice: a BorderImageSource is set but renderedBorder is {renderedBorder}; the skin paints into a zero-width border and renders nothing. Pass renderedBorder > 0.");

        return new Container
        {
            Key                    = key,
            Width                  = Length.Percent(100),
            Height                 = Length.Percent(100),
            BorderImageSource      = source,
            BorderImageWidthLeft   = sliceSrcPx,
            BorderImageWidthTop    = sliceSrcPx,
            BorderImageWidthRight  = sliceSrcPx,
            BorderImageWidthBottom = sliceSrcPx,
            BorderWidth            = renderedBorder,
            BorderImageRepeat      = repeat,
            BorderImageFill        = fill,
        };
    }

    // The silent-blank footgun: a source paints into a zero-thickness border and shows nothing.
    internal static bool ShouldWarnZeroBorder(bool hasSource, float renderedBorder)
        => hasSource && renderedBorder <= 0f;
}
