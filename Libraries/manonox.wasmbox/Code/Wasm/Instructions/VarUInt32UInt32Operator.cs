using WasmBox.Wasm.Binary;

namespace WasmBox.Wasm.Instructions {
    /// <summary>
    /// Describes an operator that takes a single 32-bit unsigned integer immediate.
    /// </summary>
    public sealed class VarUInt32UInt32Operator : Operator {
        /// <summary>
        /// Creates an operator that takes a single 32-bit unsigned integer immediate.
        /// </summary>
        /// <param name="opCode">The operator's opcode.</param>
        /// <param name="declaringType">A type that defines the operator, if any.</param>
        /// <param name="mnemonic">The operator's mnemonic.</param>
        public VarUInt32UInt32Operator(byte opCode, WasmType declaringType, string mnemonic)
            : base(opCode, declaringType, mnemonic) { }
        
        public VarUInt32UInt32Operator(byte opCode, uint index, WasmType declaringType, string mnemonic)
            : base(opCode, index, declaringType, mnemonic) { }

        /// <summary>
        /// Reads the immediates (not the opcode) of a WebAssembly instruction
        /// for this operator from the given reader and returns the result as an
        /// instruction.
        /// </summary>
        /// <param name="reader">The WebAssembly file reader to read immediates from.</param>
        /// <returns>A WebAssembly instruction.</returns>
        public override Instruction ReadImmediates( BinaryWasmReader reader ) {
            var i1 = reader.ReadVarUInt32();
            var i2 = reader.ReadVarUInt32();
            return Create( i1, i2 );
        }

        /// <summary>
        /// Creates a new instruction from this operator and the given
        /// immediate.
        /// </summary>
        /// <param name="immediate1">The first immediate.</param>
        /// <param name="immediate2">The second immediate.</param>
        /// <returns>A new instruction.</returns>
        public VarUInt32UInt32Instruction Create(uint immediate1, uint immediate2) {
            return new VarUInt32UInt32Instruction(this, immediate1, immediate2);
        }

        /// <summary>
        /// Casts the given instruction to this operator's instruction type.
        /// </summary>
        /// <param name="value">The instruction to cast.</param>
        /// <returns>The given instruction as this operator's instruction type.</returns>
        public VarUInt32UInt32Instruction CastInstruction(Instruction value) {
            return (VarUInt32UInt32Instruction)value;
        }
    }
}
