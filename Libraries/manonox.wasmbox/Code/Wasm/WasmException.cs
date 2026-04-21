using System;
using System.Runtime.Serialization;

namespace WasmBox.Wasm {
    /// <summary>
    /// A type of exception that is thrown by the Wasm namespace and its sub-namespaces.
    /// </summary>
    // [Serializable]
    public class WasmException : Exception {
		/// <summary>
		/// Initializes a new instance of the <see cref="WasmException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		public WasmException(string message)
            : base(message) { }
    }
}
