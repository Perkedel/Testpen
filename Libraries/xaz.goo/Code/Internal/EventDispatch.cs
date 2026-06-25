using System;

namespace Goo.Internal;

/// <summary>Invokes a user event handler and, if it ran, requests a rebuild.
/// Centralizes the "auto-rebuild after a handler fires" rule (the GooPanel
/// AutoRebuildOnEvents contract) for every event-host panel. Generic over the
/// payload so mouse (MousePanelEvent), wheel (Vector2), and drag (DragEvent /
/// PanelEvent) handlers all route through the same path.</summary>
internal static class EventDispatch
{
    public static void Fire<T>(Action<T>? handler, T e, Action? requestRebuild)
    {
        if (handler == null) return;
        handler.Invoke(e);
        requestRebuild?.Invoke();
    }
}
