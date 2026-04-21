using System.Collections.Generic;

namespace WasmBox.Wasm.Interpret {
    /// <summary>
    /// An importer that delegates the issue of importing values to another
    /// importer based on the module name associated with the value. That is,
    /// module names serve as "namespaces" of sorts for other importers.
    /// </summary>
    public sealed class NamespacedImporter : IImporter {
        /// <summary>
        /// Creates a linking importer.
        /// </summary>
        public NamespacedImporter() {
            ModuleImporters = new();
        }

        public NamespacedImporter(Dictionary<string, IImporter> moduleImporters) {
            ModuleImporters = moduleImporters;
        }

        public Dictionary<string, IImporter> ModuleImporters { get; set; }

        /// <summary>
        /// Registers an importer for a particular module name.
        /// </summary>
        /// <param name="moduleName">
        /// The module name to map to <paramref name="importer"/>.
        /// </param>
        /// <param name="importer">
        /// An importer to use for all imports that refer to module <paramref name="moduleName"/>.
        /// </param>
        public void RegisterImporter(string moduleName, IImporter importer) {
            ModuleImporters[moduleName] = importer;
        }

        /// <inheritdoc/>
        public FunctionDefinition ImportFunction(ImportedFunction description, FunctionType signature) {
            if (ModuleImporters.TryGetValue(description.ModuleName, out IImporter importer)) {
                return importer.ImportFunction(description, signature);
            }
            else {
                return null;
            }
        }

        /// <inheritdoc/>
        public Variable ImportGlobal(ImportedGlobal description) {
            if (ModuleImporters.TryGetValue(description.ModuleName, out IImporter importer)) {
                return importer.ImportGlobal(description);
            }
            else {
                return null;
            }
        }

        /// <inheritdoc/>
        public LinearMemory ImportMemory(ImportedMemory description) {
            if (ModuleImporters.TryGetValue(description.ModuleName, out IImporter importer)) {
                return importer.ImportMemory(description);
            }
            else {
                return null;
            }
        }

        /// <inheritdoc/>
        public FunctionTable ImportTable(ImportedTable description) {
            if (ModuleImporters.TryGetValue(description.ModuleName, out IImporter importer)) {
                return importer.ImportTable(description);
            }
            else {
                return null;
            }
        }
    }
}
