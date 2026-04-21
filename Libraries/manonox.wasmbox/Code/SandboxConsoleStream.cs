using System.IO;
using System.Text;
using WasmBox.Util.EscapeCharacters;
using WasmBox.Wasm.Text;

namespace WasmBox;

public sealed class SandboxConsoleStream : Stream {
    private Kind kind;
    private SandboxConsoleStream( Kind kind ) {
        this.kind = kind;
    }

    private enum Kind {
        Trace,
        Info,
        Warning,
        Error,
    }

    public static readonly SandboxConsoleStream Trace = new SandboxConsoleStream( Kind.Trace );
    public static readonly SandboxConsoleStream Info = new SandboxConsoleStream( Kind.Info );
    public static readonly SandboxConsoleStream Warning = new SandboxConsoleStream( Kind.Warning );
    public static readonly SandboxConsoleStream Error = new SandboxConsoleStream( Kind.Error );

    private List<byte> bytes = [];

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;

    public override long Length => bytes.Count;
    public override long Position { get => Length; set => throw new AccessViolationException(); }

    public override void SetLength( long value ) { }

    public override int Read( byte[] buffer, int offset, int count ) {
        throw new AccessViolationException();
    }
    public override long Seek( long offset, SeekOrigin origin ) {
        throw new AccessViolationException();
    }

    public override void Write( byte[] buffer, int offset, int count ) {
        bytes.AddRange( buffer[offset..(offset + count)] );
    }
    
    public void Write( string s ) {
        bytes.AddRange( Encoding.UTF8.GetBytes(s) );
    }

    public override void Flush() {
        var s = Encoding.UTF8.GetString( [.. bytes] );
        foreach ( var line in s.TrimEnd().Split( '\n' ) ) {
            OutputLine( line );
        }

        bytes.Clear();
    }

    private void OutputLine(string line) {
        switch ( kind ) {
            case Kind.Trace:
                Log.Trace( line );
                break;
            case Kind.Info:
                Log.Info( line );
                break;
            case Kind.Warning:
                Log.Warning( line );
                break;
            case Kind.Error:
                Log.Error( line );
                break;

            default:
                throw new Exception( "wtf" );
        }
    }
}
