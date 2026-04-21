namespace WasmBox.Wasi;

/// <summary>
/// Represents a type that can safely provide its size in bytes.
/// </summary>
public interface ISafeSize {
    /// <summary>
    /// Gets the size of the object in bytes.
    /// </summary>
    public static abstract uint Size { get; }
}