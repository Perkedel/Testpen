using System;

namespace Goo;

// Non-generic base so the reconciler can hold heterogeneous cells without knowing TRoot.
public abstract class Cell
{
    Action? _rebuild;

    // Set by the reconciler during DiffCell, before ExpandInto runs.
    internal void SetRebuildHook(Action? rebuild) => _rebuild = rebuild;

    // Marks the owning root dirty. Call from event handlers after mutating state.
    public void Rebuild() => _rebuild?.Invoke();

    // Writes the cell's built Blob into frame; overridden in Cell<TRoot>, monomorphized so no boxing.
    internal abstract void ExpandInto(ref Frame frame);

    // Mount a cell into a parent's child list or as a root; users never name CellElement.
    // seed runs once at first mount, before configure; configure runs every diff, so on
    // first mount a field set by both ends at the configure value.
    public static CellElement Mount<TCell>(string? key = null, Action<TCell>? seed = null, Action<TCell>? configure = null)
        where TCell : Cell, new()
    {
        Action<Cell>? boxedSeed = seed is null
            ? null
            : instance => seed((TCell)instance);
        Action<Cell>? boxed = configure is null
            ? null
            : instance => configure((TCell)instance);
        return new CellElement(typeof(TCell), static () => new TCell(), boxedSeed, boxed, key);
    }
}

public abstract class Cell<TRoot> : Cell where TRoot : struct, IBlob
{
    protected abstract TRoot Build();

    internal sealed override void ExpandInto(ref Frame frame)
    {
        TRoot root = Build();
        root.WriteTo(ref frame);   // direct, monomorphized; no boxing
    }
}

// Internal element struct the reconciler diffs. Mount<TCell> returns it; users never spell it.
public readonly record struct CellElement : IBlob
{
    public static BlobKind Kind => BlobKind.Cell;
    public string? Key { get; }

    readonly Type _cellType;
    readonly Func<Cell> _factory;
    readonly Action<Cell>? _seed;
    readonly Action<Cell>? _configure;

    internal CellElement(Type cellType, Func<Cell> factory, Action<Cell>? seed, Action<Cell>? configure, string? key)
    {
        _cellType = cellType;
        _factory = factory;
        _seed = seed;
        _configure = configure;
        Key = key;
    }

    void IBlob.WriteTo(ref Frame frame)
    {
        frame = default;
        frame.LayoutTransition = null;
        frame.Kind = BlobKind.Cell;
        frame.Key = Key;
        frame.CellType = _cellType;
        frame.CellFactory = _factory;
        frame.Seed = _seed;
        frame.Configure = _configure;
    }
}
