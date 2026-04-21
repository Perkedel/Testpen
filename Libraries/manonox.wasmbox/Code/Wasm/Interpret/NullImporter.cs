namespace WasmBox.Wasm.Interpret;

/// <summary>
/// A null implementation of an importer for WASM modules.
/// </summary>
public sealed class NullImporter : IImporter {
    private NullImporter() {}

    public static readonly NullImporter Instance = new();

    public LinearMemory ImportMemory( ImportedMemory description ) => null;
	public Variable ImportGlobal( ImportedGlobal description ) => null;
	public FunctionDefinition ImportFunction( ImportedFunction description, FunctionType signature ) => null;
	public FunctionTable ImportTable( ImportedTable description ) => null;
}
