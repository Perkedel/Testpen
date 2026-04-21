using System.IO;
using System.Text;
using WasmBox.Wasm.Binary;

namespace WasmBox.Wasm.Instructions {
    /// <summary>
    /// Describes a WebAssembly stack machine instruction that takes a
    /// variable-length 32-bit unsigned integer as immediate.
    /// </summary>
    public sealed class VarUInt32UInt32Instruction : Instruction {
        /// <summary>
        /// Creates an instruction that takes a variable-length 32-bit unsigned integer immediate.
        /// </summary>
        /// <param name="op">An appropriate operator.</param>
        /// <param name="immediate1">First decoded immediate.</param>
        /// <param name="immediate2">Second decoded immediate.</param>
        public VarUInt32UInt32Instruction( Operator op, uint immediate1, uint immediate2 ) {
            this.opValue = op;
            this.Immediate1 = immediate1;
            this.Immediate2 = immediate2;
        }

        private Operator opValue;

        /// <summary>
        /// Gets the operator for this instruction.
        /// </summary>
        /// <returns>The instruction's operator.</returns>
        public override Operator Op { get { return opValue; } }

        /// <summary>
        /// Gets or sets this instruction's immediate.
        /// </summary>
        /// <returns>The immediate value.</returns>
        public uint Immediate1 { get; set; }
        public uint Immediate2 { get; set; }

        /// <summary>
        /// Writes this instruction's immediates (but not its opcode)
        /// to the given WebAssembly file writer.
        /// </summary>
        /// <param name="writer">The writer to write this instruction's immediates to.</param>
        public override void WriteImmediatesTo( BinaryWasmWriter writer ) {
            writer.WriteVarUInt32( Immediate1 );
            writer.WriteVarUInt32( Immediate2 );
        }

        /// <summary>
        /// Writes a string representation of this instruction to the given text writer.
        /// </summary>
        /// <param name="writer">
        /// The writer to which a representation of this instruction is written.
        /// </param>
        public override void Dump(TextWriter writer) {
            Op.Dump(writer);
            writer.Write(" ");
            writer.Write(Immediate1);
            writer.Write(Immediate2);
        }
    }
}
