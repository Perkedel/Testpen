using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Goo;

public abstract class GooPanel<TRoot> : PanelComponent, IHotloadManaged where TRoot : struct, IBlob
{
    Fiber? _fiber;
    readonly BuildContext _ctx = new();
    bool _needsBuild = true;
    bool _wasMoving;

    protected abstract TRoot Build();

    public void Rebuild() => _needsBuild = true;

    /// <summary>Override to drive per-frame state (animations, polled input).
    /// Return true while a rebuild is still needed next frame (e.g. an animation is in motion).
    /// Runs every frame, before the build gate. On the frame motion stops (this returns false
    /// after the previous frame returned true) one extra rebuild fires so the final settled value paints.</summary>
    protected virtual bool Tick(float dt) => false;

    /// <summary>When true (default), a rebuild is requested automatically after any event
    /// handler on this panel's tree fires. Set false to restore fully manual Rebuild() control.</summary>
    protected virtual bool AutoRebuildOnEvents => true;

    /// <summary>Override to map tags to effects. A Blob with a matching <see cref="Container.Tag"/> and no
    /// inline <c>Effect</c> resolves its effect from here. Default targets nothing.</summary>
    protected virtual EffectMap Effects => EffectMap.Empty;

    // Hotload hook. Engine creates a fresh instance per Component on hotload, then
    // Cecil-migrates field values from the prior instance. We need to trigger a diff
    // against the new Build() output so the panel reflects edited source.
    void IHotloadManaged.Created(IReadOnlyDictionary<string, object> state) => _needsBuild = true;

    protected override void OnEnabled()
    {
        // Engine destroys Panel on disable and creates a fresh empty one on enable.
        // Drop our fiber so the next diff is a full mount against the new Panel.
        _fiber = null;
        _needsBuild = true;
        _wasMoving = false;
    }

    protected override void OnUpdate()
    {
        if (Panel == null) return;

        // Per-frame state advances every frame, independent of the build gate.
        // A motion->settled transition rebuilds once more so the final resting value paints.
        bool moving = Tick(Time.Delta);
        if (moving || _wasMoving) _needsBuild = true;
        _wasMoving = moving;
        if (!_needsBuild) return;

        var prev = BuildContext._current;
        BuildContext._current = _ctx;
        _ctx.RootRebuild = Rebuild;
        _ctx.AutoRebuildOnEvents = AutoRebuildOnEvents;
        _ctx.EffectManifest = Effects;
        try
        {
            TRoot next = Build();
            var result = Reconciler.Diff(ref _fiber, in next);
            foreach (var w in result.Warnings) Log.Warning(w);
            Applier.Apply(Panel, result.Ops);
        }
        finally
        {
            _ctx.ReturnAll();
            BuildContext._current = prev;
            _needsBuild = false;
        }
    }

    // Goo manages Panel.Children directly via Diff/Apply; skip Razor's render-tree build path.
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
    }
}
