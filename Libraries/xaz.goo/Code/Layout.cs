using Sandbox.UI;

namespace Goo;

// Maps an anchor corner to the shared JustifyContent + AlignItems + FlexDirection tuple.
public static class Layout
{
    public enum Anchor { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight, Center }

    public readonly record struct AnchorResolution(
        Justify       JustifyContent,
        Align         AlignItems,
        FlexDirection FlexDirection )
    {
        // True when content stacks bottom-up (FlexDirection.Column). Useful for slide-in
        // animations whose entries originate from below the anchor.
        public bool StacksUp => FlexDirection == FlexDirection.Column;
    }

    public static AnchorResolution ResolveAnchor( Anchor a ) => a switch
    {
        Anchor.TopLeft      => new( Justify.FlexStart, Align.FlexStart, FlexDirection.ColumnReverse ),
        Anchor.TopCenter    => new( Justify.FlexStart, Align.Center,    FlexDirection.ColumnReverse ),
        Anchor.TopRight     => new( Justify.FlexStart, Align.FlexEnd,   FlexDirection.ColumnReverse ),
        Anchor.BottomLeft   => new( Justify.FlexEnd,   Align.FlexStart, FlexDirection.Column        ),
        Anchor.BottomCenter => new( Justify.FlexEnd,   Align.Center,    FlexDirection.Column        ),
        Anchor.BottomRight  => new( Justify.FlexEnd,   Align.FlexEnd,   FlexDirection.Column        ),
        Anchor.Center       => new( Justify.Center,    Align.Center,    FlexDirection.Column        ),
        _                   => throw new System.ArgumentOutOfRangeException( nameof( a ) ),
    };
}
