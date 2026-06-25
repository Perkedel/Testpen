# HitShapes

non-rectangular UI regions in s&box

HitShapes wraps a shape resolver in a state machine that turns whole-panel mouse events into per-slot `OnSlotEnter / OnSlotLeave / OnSlotClick` callbacks.

Depends only on `Vector2` and `MousePanelEvent`

## Quick start

```csharp
using HitShapes;

readonly SlotDispatcher _dispatcher = new(HitShape.Radial(8, innerRatio: 0.4f))
{
    OnSlotEnter = slot      => Highlight(slot),
    OnSlotLeave = slot      => Unhighlight(slot),
    OnSlotClick = (slot, e) => OnPicked(slot),
};
```

Wire your panel's mouse events to the dispatcher. In Razor (verified working in `Code/Demo/RazorHitShapeDemo.razor`):

```razor
@inherits PanelComponent
@using HitShapes
@using Sandbox.UI

<root @onmousemove=@OnHover
      @onmouseout=@OnUnhover
      @onclick=@OnClicked></root>

@code
{
    SlotDispatcher _dispatcher;

    protected override void OnEnabled()
    {
        base.OnEnabled();
        _dispatcher = new SlotDispatcher( HitShape.Radial( 8 ) )
        {
            OnSlotEnter = _ => StateHasChanged(),
            OnSlotLeave = _ => StateHasChanged(),
            OnSlotClick = ( slot, e ) => Log.Info( $"slot {slot} clicked" ),
        };
    }

    void OnHover( PanelEvent e )    => _dispatcher?.HandleMouseMove( (MousePanelEvent)e, Panel );
    void OnUnhover( PanelEvent e )  => _dispatcher?.HandleMouseLeave( (MousePanelEvent)e );
    void OnClicked( PanelEvent e )  => _dispatcher?.HandleClick( (MousePanelEvent)e, Panel );
}
```

Use named methods (not inline `@(e => ...)` lambdas); anonymous methods lose identity across hotload. Cast `PanelEvent` to `MousePanelEvent` inside the method; the engine guarantees all mouse-event names fire `MousePanelEvent`. Initialise the dispatcher in `OnEnabled` so each enable cycle starts clean.

**Pass `Panel` as the receiver** to the `HandleMouseMove(e, Panel)` / `HandleClick(e, Panel)` overloads. Sandbox.UI events bubble: a Razor handler on `<root>` may receive events whose `e.Target` is a descendant or a transient external panel (visible on quick re-entry through the panel's edge). `LocalPosition` is always in the receiver's frame, so the receiver overloads pair it with `Panel.Box.Rect.Size` and resolve correctly. The single-arg overloads use `e.Target.Box` and are safe only when children are `PointerEvents.None` AND no bubbled cross-panel target is possible (the Goo demos satisfy both).

For hover state, the `OnSlotEnter` / `OnSlotLeave` callbacks should call `StateHasChanged()` to refresh the highlighted slot. If the dispatcher survives panel recreation, call `_dispatcher.Reset()` to clear stale hover state.

## Built-in shapes

| Factory | Shape | Slot ids |
|---|---|---|
| `Radial(slots, innerRatio?, outerRatio?)` | Wheel of equal wedges | 0..slots-1, clockwise from 12 o'clock |
| `RectGrid(cols, rows)` | Uniform grid | row-major, top-left = 0 |
| `Polygon(verts)` | One polygon, unit coords | always 0 if inside |
| `Polygons(polys)` | Many polygons | index of first containing polygon |
| `Custom(slotCount, nativeSize, fn)` | Resolver de-scaled to a fixed pixel frame | whatever you return |
| `CustomRaw(slotCount, fn)` | Resolver receives raw engine-frame coords | whatever you return |
| `Union(a, b)` | A's slots, then B's offset by `A.SlotCount` | A wins on overlap |
| `Intersect(a, b)` | A's slot, only where B also resolves | A's slot count |
| `Difference(a, b)` | A's slot, where B does NOT resolve | A's slot count |

`Radial` and `RectGrid` are cached by parameter tuple. Others are not; hold a `static readonly` field or keep the dispatcher across rebuilds.

## Coordinate frames

`LocalPosition` and `Box.Rect.Size` come through in the engine's rendered frame, which may diverge from your authored pixels under UI scaling or world-space rendering.

1. **Scale-invariant** (`CustomRaw`): express everything as fractions of `size`.
2. **Fixed native frame** (`Custom`): wrapper de-scales to your `nativeSize` first. Use when comparing against fixed pixel constants (backing texture, sprite atlas).

The trap is `CustomRaw` plus a fixed pixel constant: cursor drifts from cells when rendered size diverges from authored size.

## Demos

`Code/Demo/*UI.cs`, one per factory. `RadialWheelUI.cs` is the simplest worked example.
