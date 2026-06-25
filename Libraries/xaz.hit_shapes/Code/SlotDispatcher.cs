using System;
using Sandbox;
using Sandbox.UI;

namespace HitShapes;

/// <summary>Holds an <see cref="IHitShape"/> and tracks per-slot enter/leave/click transitions.</summary>
public sealed class SlotDispatcher
{
    readonly IHitShape _shape;
    readonly IGeometricHitShape? _geometric;
    int? _currentSlot;
    float? _lastAngleDeg;
    float? _lastDistanceNormalized;

    public SlotDispatcher(IHitShape shape)
    {
        _shape = shape ?? throw new ArgumentNullException(nameof(shape));
        _geometric = shape as IGeometricHitShape;
    }

    public IHitShape Shape => _shape;
    public int? CurrentSlot => _currentSlot;

    /// <summary>Cursor angle from the most recent hover update, in degrees,
    /// 0 = up, clockwise positive, [0, 360). Null when the cursor is outside
    /// the shape or the underlying <see cref="IHitShape"/> does not provide
    /// geometric state (only radial shapes do today).</summary>
    public float? LastAngleDeg => _lastAngleDeg;

    /// <summary>Cursor distance from center as a 0..1 fraction of the shape's
    /// outer radius, from the most recent hover update. Null when the cursor
    /// is outside the shape or the underlying <see cref="IHitShape"/> does not
    /// provide geometric state.</summary>
    public float? LastDistanceNormalized => _lastDistanceNormalized;

    public Action<int> OnSlotEnter { get; init; } = _ => { };
    public Action<int> OnSlotLeave { get; init; } = _ => { };
    public Action<int, MousePanelEvent> OnSlotClick { get; init; } = (_, _) => { };

    public void HandleMouseEnter(MousePanelEvent e) => UpdateHoverFrom(e);
    public void HandleMouseMove(MousePanelEvent e) => UpdateHoverFrom(e);

    /// <summary>Hover update using explicit receiver size. Safe under event bubbling.</summary>
    public void HandleMouseEnter(MousePanelEvent e, Panel receiver) => UpdateHoverAt(e.LocalPosition, receiver.Box.Rect.Size);
    public void HandleMouseMove(MousePanelEvent e, Panel receiver) => UpdateHoverAt(e.LocalPosition, receiver.Box.Rect.Size);

    public void HandleMouseLeave(MousePanelEvent e)
    {
        _lastAngleDeg = null;
        _lastDistanceNormalized = null;
        if (_currentSlot is int oldSlot)
        {
            _currentSlot = null;
            OnSlotLeave?.Invoke(oldSlot);
        }
    }

    public void HandleClick(MousePanelEvent e)
    {
        var size = e.Target?.Box.Rect.Size ?? Vector2.Zero;
        var slot = _shape.Resolve(e.LocalPosition, size);
        if (slot is int s) OnSlotClick?.Invoke(s, e);
    }

    /// <summary>Click using explicit receiver size. Safe under event bubbling.</summary>
    public void HandleClick(MousePanelEvent e, Panel receiver)
    {
        var slot = _shape.Resolve(e.LocalPosition, receiver.Box.Rect.Size);
        if (slot is int s) OnSlotClick?.Invoke(s, e);
    }

    /// <summary>Drive the hover state machine without a MousePanelEvent.</summary>
    public void UpdateHoverAt(Vector2 localPosition, Vector2 panelSize)
    {
        int? newSlot;
        if (_geometric is not null)
        {
            newSlot = _geometric.ResolveWithGeometry(localPosition, panelSize, out _lastAngleDeg, out _lastDistanceNormalized);
        }
        else
        {
            newSlot = _shape.Resolve(localPosition, panelSize);
            _lastAngleDeg = null;
            _lastDistanceNormalized = null;
        }

        if (newSlot == _currentSlot) return;

        var oldSlot = _currentSlot;
        _currentSlot = newSlot;
        if (oldSlot is int leaving) OnSlotLeave?.Invoke(leaving);
        if (newSlot is int entering) OnSlotEnter?.Invoke(entering);
    }

    /// <summary>Clear hover state. Call after panel recreation if the dispatcher survives it.</summary>
    public void Reset()
    {
        _currentSlot = null;
        _lastAngleDeg = null;
        _lastDistanceNormalized = null;
    }

    void UpdateHoverFrom(MousePanelEvent e)
    {
        var size = e.Target?.Box.Rect.Size ?? Vector2.Zero;
        UpdateHoverAt(e.LocalPosition, size);
    }
}
