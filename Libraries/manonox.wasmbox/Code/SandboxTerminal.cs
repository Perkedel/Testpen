using System;
using System.IO;
using System.Text;
using WasmBox.Pixie.Terminal.Devices;
using WasmBox.Wasm.Interpret;

namespace WasmBox;

public class SandboxTerminal : TextWriterTerminal {
    public SandboxTerminal() : base(new SandboxLogTextWriter(), 60, NoStyleManager.Instance, Encoding.UTF8) { }
}

internal class SandboxLogTextWriter : TextWriter {
    public override Encoding Encoding => Encoding.UTF8;

    private StringBuilder buffer = new();

    public override void Write( char value ) {
        buffer.Append( value );
    }

	public override void Write( string value ) {
        buffer.Append( value );
	}

    public override void WriteLine() {
        buffer.Append( '\n' );
        Flush();
	}

    public override void WriteLine( string value ) {
        buffer.Append( value );
        WriteLine();
	}

    public override void Flush() {
        Log.Info( buffer.ToString() );
        buffer = new();
    }
}


