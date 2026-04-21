using WasmBox.Wasm.Interpret;

namespace WasmThrobbing;

[Icon( "code" )]
[Group( "WASM" )]
public sealed class WatTester : Component {
	[Property]
	[TextArea]
	public string Source { get; set; } = "(module\n (func (export \"main\") (result i32 f32)\n  f32.const 3.1415\n  i32.const 3\n  i32.const 5\n  i32.add\n )\n)\n";

	[Property]
	public string EntryPoint { get; set; } = "main";

	protected override void OnStart() {
		WasmFile wasmFile = new Assembler( SandboxLog.Instance ).AssembleModule( Source );
        Log.Info( $"Running WAT" );

        ModuleInstance module = ModuleInstance.Instantiate( wasmFile );

		try {
			List<object> results = [.. module.ExportedFunctions[EntryPoint].Invoke( [] )];
			Log.Info( $"Results: {string.Join(", ", from x in results select '\''+x.ToString()+'\'')}" );
		}
		catch ( TrapException e ) {
			Log.Warning( $"Trap: {e}" );
		}
	}
}
