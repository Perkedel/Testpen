using Sandbox;
using Sandbox.UI;

namespace Goo;

// Factories for full-bleed HUD/overlay roots (absolute, full-screen, Column, pointer-events off).
public static class Hud
{
    /// <summary>Full-screen HUD root: Column layout, pointer-events off. For shader overlays use Hud.Fill().</summary>
    public static Container Overlay() => new()
    {
        Position      = PositionMode.Absolute,
        Top           = 0,
        Left          = 0,
        Width         = Length.Percent( 100 ),
        Height        = Length.Percent( 100 ),
        FlexDirection = FlexDirection.Column,
        PointerEvents = PointerEvents.None,
    };

    /// <summary>Absolute, 100% x 100%, pointer-through fill layer. Resolves inside the nearest positioned ancestor; add Position=Relative to that ancestor if needed. Style fields the factory sets (Position, Top, Left, Width, Height, PointerEvents) cannot be overridden via <c>with</c> - first-declared wins; build a Container directly for an interactive full-bleed layer.</summary>
    public static Container Fill() => new()
    {
        Position      = PositionMode.Absolute,
        Top           = 0,
        Left          = 0,
        Width         = Length.Percent( 100 ),
        Height        = Length.Percent( 100 ),
        PointerEvents = PointerEvents.None,
    };

    /// <summary>Absolute, all four edges pinned to zero. Assign PointerEvents explicitly - scrims typically toggle between None and All on open/close. The factory does not set PointerEvents, so it is overridable via <c>with</c> (unlike Fill, which sets it).</summary>
    public static Container Scrim( Color? color = null ) => new()
    {
        Position        = PositionMode.Absolute,
        Top             = 0,
        Left            = 0,
        Right           = 0,
        Bottom          = 0,
        BackgroundColor = color,
    };

    /// <summary>Full-bleed absolute image layer. Pass texture=null to load by path; path is ignored when texture is non-null. Style fields the factory sets (Position, Top, Left, Width, Height, PointerEvents) cannot be overridden via <c>with</c> - first-declared wins; build a Container directly for an interactive full-bleed layer. Must be called from within Build().</summary>
    public static Container Wallpaper( Texture? texture, string? path )
    {
        var c = Fill();
        c.Children.Add( new Image
        {
            Width      = Length.Percent( 100 ),
            Height     = Length.Percent( 100 ),
            FlexShrink = 0,
            Texture    = texture,
            Path       = texture is null ? path : null,
            ObjectFit  = ObjectFit.Fill,
        } );
        return c;
    }

    /// <summary>Full-bleed pointer-through cell that pins content to an anchor corner. Wrapper is always FlexDirection.Column; ResolveAnchor's ColumnReverse belongs on an inner stacking column, never here (top anchors would render at the bottom).</summary>
    public static Container Anchored( Layout.Anchor anchor, Container content, Length? padding = default, string? key = null )
    {
        var r = Layout.ResolveAnchor( anchor );
        return new Container
        {
            Key            = key,
            Position       = PositionMode.Absolute,
            Top            = 0,
            Left           = 0,
            Width          = Length.Percent( 100 ),
            Height         = Length.Percent( 100 ),
            Padding        = padding,
            PointerEvents  = PointerEvents.None,
            FlexDirection  = FlexDirection.Column,
            JustifyContent = r.JustifyContent,
            AlignItems     = r.AlignItems,
            Children       = { content },
        };
    }

    /// <summary>A flex-grow spacer that pushes siblings apart. Sets only FlexGrow = 1; all other fields are caller-controlled.</summary>
    public static Container Spacer() => new() { FlexGrow = 1 };

    /// <summary>A 1px full-width horizontal rule. Defaults to a low-alpha white line; pass a color to override. Sets Height, Width, and BackgroundColor.</summary>
    public static Container Divider( Color? color = null ) => new()
    {
        Height          = Px.Of( 1 ),
        Width           = Length.Percent( 100 ),
        // 8 percent white: legible on dark glass and solid dark backgrounds, tuned visually.
        BackgroundColor = color ?? new Color( 1f, 1f, 1f, 0.08f ),
    };
}
