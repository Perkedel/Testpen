using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using WasmBox.Wasi.Data;
using WasmBox.Wasm.Interpret;

namespace WasmBox.Wasi;

public class WasiInstance : IImporter {
    const string ModuleName = "wasi_snapshot_preview1";

    public WasiInstance() {
        // This is a godsend: https://wasix.org/docs/api-reference

        predefinedImporter.DefineFunction( "fd_write", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (fd, iovs_ptr, iovsLen, nwritten_ptr) = ((int)inputs[0], (uint)(int)inputs[1], (int)inputs[2], (uint)(int)inputs[3]);

                Stream stream;
                switch ( fd ) {
                    case 0:
                        return [Errno.EIO];
                    case 1:
                        stream = StdOut;
                        break;
                    case 2:
                        stream = StdErr;
                        break;
                    default:
                        return [Errno.EPERM];
                }

                var memory = context.Module.Memories[0];
                var memoryInt32 = memory.Int32;
                int written = 0;
                for ( uint i = 0; i < iovsLen; i++ ) {
                    var offset = i * __wasi_ciovec_t.Size;
                    var iovec = new __wasi_ciovec_t() {
                        Pointer = memoryInt32[iovs_ptr + offset],
                        Length = memoryInt32[iovs_ptr + offset + 4],
                    };

                    var buffer = memory[iovec];
                    stream.Write( buffer );
                    written += iovec.Length;
                }
                stream.Flush();

                memoryInt32[nwritten_ptr] = written;
                return [Errno.SUCCESS];
            } ) );

        predefinedImporter.DefineFunction( "fd_read", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (fd, iovs_ptr, iovsLen, nread_ptr) = ((int)inputs[0], (uint)(int)inputs[1], (int)inputs[2], (uint)(int)inputs[3]);

                Stream stream;
                switch ( fd ) {
                    case 0:
                        stream = StdIn;
                        break;
                    default:
                        return [Errno.EPERM];
                }

                var memory = context.Module.Memories[0];
                var memoryInt32 = memory.Int32;
                int read = 0;
                for ( uint i = 0; i < iovsLen; i++ ) {
                    var offset = i * __wasi_ciovec_t.Size;
                    var iovec = new __wasi_ciovec_t() {
                        Pointer = memoryInt32[iovs_ptr + offset],
                        Length = memoryInt32[iovs_ptr + offset + 4],
                    };

                    var buffer = memory[iovec];
                    var tempBuffer = new byte[iovec.Length];
                    int bytesRead = stream.Read( tempBuffer, 0, iovec.Length );

                    tempBuffer.CopyTo( buffer );
                    read += bytesRead;
                }

                memoryInt32[nread_ptr] = read;
                return [Errno.SUCCESS];
            } ) );


        predefinedImporter.DefineFunction( "environ_sizes_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (environ_count, environ_buf_size) = ((uint)(int)inputs[0], (uint)(int)inputs[1]);
                var memoryInt32 = context.Module.Memories[0].Int32;
                memoryInt32[environ_count] = EnvironmentVariables.Count;
                memoryInt32[environ_buf_size] = EnvironmentVariableBufferSize;
                return [Errno.SUCCESS];
            } ) );

        predefinedImporter.DefineFunction( "environ_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (environ, environ_buf) = ((uint)(int)inputs[0], (uint)(int)inputs[1]);
                var memory = context.Module.Memories[0];
                var memoryInt32 = memory.Int32;

                uint i = 0;
                uint offset = environ_buf;
                foreach ( var kv in EnvironmentVariables ) {
                    string env = $"{kv.Key}={kv.Value}";
                    byte[] envBytes = Encoding.UTF8.GetBytes( env + '\0' );
                    var length = (uint)envBytes.Length;
                    memoryInt32[environ + i * 4] = (int)offset;
                    memory[(int)offset..(int)(offset + length)] = envBytes;

                    offset += length;
                    i++;
                }
                return [Errno.SUCCESS];
            } ) );

        predefinedImporter.DefineFunction( "args_sizes_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (argc, argv_buf_size) = ((uint)(int)inputs[0], (uint)(int)inputs[1]);
                var memoryInt32 = context.Module.Memories[0].Int32;
                memoryInt32[argc] = Arguments.Count;
                memoryInt32[argv_buf_size] = ArgumentsBufferSize;
                return [Errno.SUCCESS];
            } ) );

        predefinedImporter.DefineFunction( "args_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (argv, argv_buf) = ((uint)(int)inputs[0], (uint)(int)inputs[1]);
                var memory = context.Module.Memories[0];
                var memoryInt32 = memory.Int32;

                uint i = 0;
                uint offset = argv_buf;
                foreach ( var arg in Arguments ) {
                    byte[] argBytes = Encoding.UTF8.GetBytes( $"{arg}\0" );
                    var length = (uint)argBytes.Length;
                    memoryInt32[argv + i * 4] = (int)offset;
                    memory[(int)offset..(int)(offset + length)] = argBytes;

                    offset += length;
                    i++;
                }
                return [Errno.SUCCESS];
            } ) );

        predefinedImporter.DefineFunction( "clock_time_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int64, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (clockId, precision, time_ptr) = ((int)inputs[0], (long)inputs[1], (uint)(int)inputs[2]);
                var memory = context.Module.Memories[0];
                var memoryInt64 = memory.Int64;

                long nano = 10000L * Stopwatch.GetTimestamp();
                nano /= TimeSpan.TicksPerMillisecond;
                nano *= 100L;
                memoryInt64[time_ptr] = nano;
                return [Errno.SUCCESS];
            } ) );

        predefinedImporter.DefineFunction( "clock_res_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (clockId, resolution_ptr) = ((int)inputs[0], (uint)(int)inputs[1]);
                var memory = context.Module.Memories[0];
                var memoryInt32 = memory.Int32;

                memoryInt32[resolution_ptr] = 100;
                return [Errno.SUCCESS];
            } ) );

        predefinedImporter.DefineFunction( "random_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                var (buf_ptr, buf_len) = ((uint)(int)inputs[0], (int)inputs[1]);
                var memory = context.Module.Memories[0];
                for ( uint i = 0; i < buf_len; i++ )
                    memory[buf_ptr + i] = (byte)Random.Shared.Next( 0, 256 );
                return [Errno.SUCCESS];
            } ) );

        predefinedImporter.DefineFunction( "proc_exit", new DelegateFunctionDefinition(
            [WasmValueType.Int32],
            [],
            ( context, inputs ) => {
                throw new ProcessExitTrapException( (int)inputs[0] );
            } ) );

        predefinedImporter.DefineFunction( "sched_yield", new DelegateFunctionDefinition(
            [],
            [WasmValueType.Int32],
            ( context, inputs ) => {
                return [Errno.SUCCESS];
            }
        ) );


        // Idk

        predefinedImporter.DefineFunction( "proc_raise", new DelegateFunctionDefinition(
            [WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );



        // These could be implemented for fd of 0/1/2 (stdin/stdout/stderr), but, uhm, too lazy to do dat

        predefinedImporter.DefineFunction( "fd_pwrite", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_pread", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_seek", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_tell", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );


        // File system stuff: Not implemented / Not allowed

        predefinedImporter.DefineFunction( "fd_advise", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_allocate", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_close", new DelegateFunctionDefinition(
            [WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_datasync", new DelegateFunctionDefinition(
            [WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_fdstat_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_fdstat_set_flags", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_fdstat_set_rights", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_filestat_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_filestat_set_size", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_filestat_set_times", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_prestat_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_prestat_dir_name", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_readdir", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_renumber", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "fd_sync", new DelegateFunctionDefinition(
            [WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_create_directory", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_filestat_get", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_filestat_set_times", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_link", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_open", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_readlink", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_remove_directory", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_rename", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_symlink", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "path_unlink_file", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "poll_oneoff", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );


        // Socket stuff : Not Implemented / Not Allowed

        predefinedImporter.DefineFunction( "sock_accept", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "sock_recv", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "sock_send", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );

        predefinedImporter.DefineFunction( "sock_shutdown", new DelegateFunctionDefinition(
            [WasmValueType.Int32, WasmValueType.Int32],
            [WasmValueType.Int32],
            ( context, inputs ) => [Errno.EPERM] ) );



        namespacedImporter.RegisterImporter( ModuleName, predefinedImporter );
    }

    public IImporter Importer => this;

    public Stream StdIn { get; set; } = new MemoryStream();
    public Stream StdOut { get; set; } = SandboxConsoleStream.Info;
    public Stream StdErr { get; set; } = SandboxConsoleStream.Warning;


    public void SetStdInText(string s) {
        StdIn.Flush();
        StdIn.SetLength( 0 );
        StdIn.Write( Encoding.UTF8.GetBytes( s ) );
        StdIn.Seek( 0, SeekOrigin.Begin );
    }

    public List<string> Arguments = [];
    private int ArgumentsBufferSize
        => Arguments.Select( x => Encoding.UTF8.GetByteCount( $"{x}\0" ) ).Sum();

    public Dictionary<string, string> EnvironmentVariables = [];
    private int EnvironmentVariableBufferSize
        => EnvironmentVariables.Select( kv => Encoding.UTF8.GetByteCount( $"{kv.Key}={kv.Value}\0" ) ).Sum();

    private NamespacedImporter namespacedImporter = new();
    private PredefinedImporter predefinedImporter = new();

    public FunctionDefinition ImportFunction( ImportedFunction description, FunctionType signature )
        => namespacedImporter.ImportFunction( description, signature );

    public Variable ImportGlobal( ImportedGlobal description )
        => namespacedImporter.ImportGlobal( description );

    public LinearMemory ImportMemory( ImportedMemory description )
        => namespacedImporter.ImportMemory( description );

    public FunctionTable ImportTable( ImportedTable description )
        => namespacedImporter.ImportTable( description );
}
