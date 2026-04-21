using WasmBox.Wasm.Interpret;
using WasmBox.Wasi;
using System.IO;
using System.Text;

namespace WasmThrobbing;

[Icon( "code" )]
[Group( "WASM" )]
public sealed class WasmTester : Component {
	[Property]
	[FilePath( Extension = "wasm" )]
	public string FilePath { get; set; } = "";
	public bool IsFilePathSet => FilePath != null && FilePath.Length > 0;


	[Property]
	public Dictionary<string, string> EnvironmentVariables { get; set; } = [];

	[Property]
	public List<string> Arguments { get; set; } = [];

	[Property]
	[TextArea]
	public string StandardInput { get; set; } = "";

	protected override void OnStart() {
		if ( FilePath.Length == 0 ) {
			Log.Warning( "WasmTester: Empty file path" );
			return;
		}

		WasmFile wasmFile = WasmFile.ReadBinary( FileSystem.Mounted.ReadAllBytes( FilePath ) );
		Log.Info( $"Running {FilePath}" );

		var wasi = new WasiInstance() {
			EnvironmentVariables = EnvironmentVariables,
			Arguments = Arguments
		};

        ModuleInstance module = ModuleInstance.Instantiate( wasmFile, wasi.Importer );

		if ( StandardInput.Length > 0 ) {
			wasi.SetStdInText( StandardInput );
		}

		int exitCode = 0;
        try {
            module.ExportedFunctions["_start"].Invoke( [] );
        }
        catch ( ProcessExitTrapException e ) {
            exitCode = e.ExitCode;
        }
        catch ( TrapException e ) {
            Log.Warning( $"Trap: {e}" );
        }
		
		Log.Info( $"Exit code: {exitCode}" );
	}
}
