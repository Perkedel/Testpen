using WasmBox.Pixie;
using WasmBox.Pixie.Terminal;

namespace WasmBox;

/// <summary>
/// A log implementation that logs messages to the sbox console.
/// </summary>
public sealed class SandboxLog : ILog
{
    private SandboxLog() { }
    
    public static readonly SandboxLog Instance = new();
    public TerminalLog TerminalLog { get; private set; } = new TerminalLog( new SandboxTerminal() );

	public void Log( LogEntry entry ) {
        TerminalLog.Log( entry );
    }
}