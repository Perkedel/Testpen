using System;
using Sandbox;
using Sandbox.UI;

namespace Goo;

// Composed control factories (compose-list widgets). Stateless: each returns a
// Container subtree, following the Shapes/Skins idiom.
public static partial class Controls
{
    /// <summary>Goo primitive button: unstyled click target wrapping a text label. Pass a null onClick for a display-only button (no handler is wired). Distinct from Components.Button in the app project, which applies brand tokens and visual chrome. Use this to build custom-styled buttons without inheriting app-level styling. Style fields the factory sets (PointerEvents, FlexDirection, AlignItems, JustifyContent, and conditionally HoverBackgroundColor) cannot be overridden via <c>with</c> - first-declared wins; see Hud.Fill for the same constraint.</summary>
    public static Container Button(
        string  label,
        Action? onClick    = null,
        Color?  hoverColor = null,
        string? key        = null )
    {
        return new Container
        {
            Key                  = key,
            PointerEvents        = PointerEvents.All,
            FlexDirection        = FlexDirection.Row,
            AlignItems           = Align.Center,
            JustifyContent       = Justify.Center,
            HoverBackgroundColor = hoverColor,
            OnClick              = onClick is null ? null : _ => onClick(),
            Children             = { new Text( label ) },
        };
    }


    // Maps a cursor X (in the root's rendered pixel frame) to a snapped, clamped value.
    // Ratio-based (localX / width) so it is scale-invariant; engine UI scaling changes
    // the absolute pixels but not the ratio (engine-fact-mousepanelevent-rendered-frame).
    internal static float ValueAt(float localX, float width, float min, float max, float step)
    {
        if (width <= 0f || max <= min) return min;
        float norm = Math.Clamp(localX / width, 0f, 1f);
        float v = min + norm * (max - min);
        if (step > 0f) v = MathF.Round(v / step) * step;
        return Math.Clamp(v, min, max);
    }

    // Opacity multiplier applied to a Container when Disabled = true.
    public const float DisabledOpacity = 0.45f;

    // Canonical disabled dimming; multiplies any declared opacity.
    internal static float ResolveDisabledOpacity(float? declaredOpacity)
        => (declaredOpacity ?? 1f) * DisabledOpacity;

    // Dev-diagnostic sink for the degenerate max<=min case (mirrors Skins.OnZeroBorder).
    public static Action<string>? OnDegenerateRange;

    static readonly Color TrackBg = new( 0.28f, 0.28f, 0.34f, 1f );
    static readonly Color FillBg  = new( 0.55f, 0.78f, 0.95f, 1f );
    static readonly Color ThumbBg = new( 0.95f, 0.96f, 1.00f, 1f );

    // Controlled, stateless horizontal slider: the caller owns value, updates it in onChanged, and re-renders. Press-to-jump + drag within the bar (no engine move-capture; engine-fact-sbox-ui-input-and-drag); key pins reconciler identity so the Active pointer survives per-move re-renders.
    // disabled: when true, sets Disabled on the root container (forces pointer-off and opacity dim); onChanged is not called. Callers that wrap this in their own disabled container must pass disabled=true here and omit Disabled on the wrapper to avoid double-dim (0.45 x 0.45).
    public static Container Slider(
        float value, float min, float max, float step,
        Action<float> onChanged, string? key = null, bool disabled = false )
    {
        if ( max <= min )
            OnDegenerateRange?.Invoke( $"Goo.Controls.Slider: max ({max}) <= min ({min}); rendering an inert track." );

        float pct = (max > min ? Math.Clamp( (value - min) / (max - min), 0f, 1f ) : 0f) * 100f;

        void Set( MousePanelEvent e )
            => onChanged( ValueAt( e.LocalPosition.x, e.Target.Box.Rect.Size.x, min, max, step ) );

        return new Container
        {
            Key            = key,
            Disabled       = disabled ? (bool?)true : null,
            PointerEvents  = PointerEvents.All,
            Width          = Length.Percent( 100 ),
            Height         = 20f,
            FlexDirection  = FlexDirection.Column,
            JustifyContent = Justify.Center,
            OnMouseDown    = Set,                                   // press jumps to position
            OnMouseMove    = e => { if ( e.Target.HasActive ) Set( e ); }, // drag while pressed
            Children =
            {
                // track/fill/thumb are inert: handler-less, variant-less panels resolve to
                // PointerEvents.None, so the slider parent stays e.Target for press/drag.
                new Container
                {
                    Key             = "track",
                    Position        = PositionMode.Relative,        // positioned ancestor for fill/thumb
                    Width           = Length.Percent( 100 ),
                    Height          = 7f,
                    BorderRadius    = 4f,
                    BackgroundColor = TrackBg,
                    Children =
                    {
                        new Container
                        {
                            Key             = "fill",
                            Position        = PositionMode.Absolute,
                            Left            = 0f,
                            Height          = Length.Percent( 100 ),
                            Width           = Length.Percent( pct ),
                            BorderRadius    = 4f,
                            BackgroundColor = FillBg,
                        },
                        new Container
                        {
                            Key             = "thumb",
                            Position        = PositionMode.Absolute,
                            Left            = Length.Percent( pct ),
                            Top             = Length.Percent( 50 ),
                            Width           = 16f,
                            Height          = 16f,
                            BorderRadius    = Length.Percent( 50 ),
                            BackgroundColor = ThumbBg,
                            // Center the thumb on the (x = value, y = track-mid) point.
                            // Length.Percent returns Length?, never null here; ?? default unwraps
                            // it the same way Px.Of does (the codebase's nullable-Length idiom).
                            Transform       = PanelTransform.Translate( Length.Percent( -50 ) ?? default, Length.Percent( -50 ) ?? default ),
                        },
                    },
                },
            },
        };
    }
}
