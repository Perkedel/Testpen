#pragma warning disable IDE1006

using System;

namespace WasmBox.Wasi.Data;

public struct __wasi_ciovec_t : ISafeSize {
    public static uint Size => 8;

    public required int Pointer { get; set; }
    public required int Length { get; set; }

    public override readonly string ToString()
        => $"__wasi_ciovec_t{{p:{Pointer}, l: {Length}}}";


    public static implicit operator Range(__wasi_ciovec_t ciovec)
        => new(ciovec.Pointer, ciovec.Pointer + ciovec.Length);
}