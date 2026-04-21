using System;
using System.Collections.Generic;

namespace WasmBox.Wasm.Interpret {
    /// <summary>
    /// An importer implementation that imports predefined values.
    /// </summary>
    public sealed class PredefinedImporter : IImporter {
        /// <summary>
        /// Creates a new importer.
        /// </summary>
        public PredefinedImporter() {
            FunctionDefinitions = [];
            VariableDefinitions = [];
            MemoryDefinitions = [];
            TableDefinitions = [];
        }

        public Dictionary<string, FunctionDefinition> FunctionDefinitions { get; set; }
        public Dictionary<string, Variable> VariableDefinitions { get; set; }
        public Dictionary<string, LinearMemory> MemoryDefinitions { get; set; }
        public Dictionary<string, FunctionTable> TableDefinitions { get; set; }
        
        /// <summary>
        /// Maps the given name to the given function definition.
        /// </summary>
        /// <param name="name">The name to define.</param>
        /// <param name="definition">The function definition.</param>
        public void DefineFunction( string name, FunctionDefinition definition ) {
            FunctionDefinitions[name] = definition;
        }

        /// <summary>
        /// Maps the given name to the given variable.
        /// </summary>
        /// <param name="name">The name to define.</param>
        /// <param name="definition">The variable definition.</param>
        public void DefineVariable(string name, Variable definition) {
            VariableDefinitions[name] = definition;
        }

        /// <summary>
        /// Maps the given name to the given memory.
        /// </summary>
        /// <param name="name">The name to define.</param>
        /// <param name="definition">The memory definition.</param>
        public void DefineMemory(string name, LinearMemory definition) {
            MemoryDefinitions[name] = definition;
        }

        /// <summary>
        /// Maps the given name to the given table.
        /// </summary>
        /// <param name="name">The name to define.</param>
        /// <param name="definition">The table definition.</param>
        public void DefineTable(string name, FunctionTable definition) {
            TableDefinitions[name] = definition;
        }

        /// <summary>
        /// Includes the definitions from the given importer in this importer.
        /// </summary>
        /// <param name="importer">The importer to include.</param>
        public void IncludeDefinitions(PredefinedImporter importer) {
			CopyDefinitions( importer.FunctionDefinitions, this.FunctionDefinitions);
			CopyDefinitions( importer.VariableDefinitions, this.VariableDefinitions);
			CopyDefinitions( importer.MemoryDefinitions, this.MemoryDefinitions);
			CopyDefinitions( importer.TableDefinitions, this.TableDefinitions);
        }

        private static void CopyDefinitions<T>(
            Dictionary<string, T> sourceDefinitions,
            Dictionary<string, T> targetDefinitions) {
            foreach (var pair in sourceDefinitions) {
                targetDefinitions[pair.Key] = pair.Value;
            }
        }

        private static T ImportOrDefault<T>(ImportedValue value, Dictionary<string, T> definitions) {
            T result;
            if (definitions.TryGetValue(value.FieldName, out result)) {
                return result;
            }
            else {
                return default(T);
            }
        }

        /// <inheritdoc/>
        public FunctionDefinition ImportFunction(ImportedFunction description, FunctionType signature) {
            return ImportOrDefault( description, FunctionDefinitions);
        }

        /// <inheritdoc/>
        public Variable ImportGlobal(ImportedGlobal description) {
            return ImportOrDefault( description, VariableDefinitions);
        }

        /// <inheritdoc/>
        public LinearMemory ImportMemory(ImportedMemory description) {
            return ImportOrDefault( description, MemoryDefinitions);
        }

        /// <inheritdoc/>
        public FunctionTable ImportTable(ImportedTable description) {
            return ImportOrDefault( description, TableDefinitions);
        }
    }
}
