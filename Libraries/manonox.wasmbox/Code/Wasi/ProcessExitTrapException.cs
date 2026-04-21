using System;

namespace WasmBox.Wasi;

/// <summary>
/// Exception thrown to indicate a WASI process exit with a specific exit code.
/// </summary>
public class ProcessExitTrapException : Exception {
    /// <summary>
    /// Gets the exit code associated with the process exit.
    /// </summary>
    public int ExitCode { get; }

    public ProcessExitTrapException( int exitCode )
        : base( $"WASI process exited with code {exitCode}." ) {
        ExitCode = exitCode;
    }
}
