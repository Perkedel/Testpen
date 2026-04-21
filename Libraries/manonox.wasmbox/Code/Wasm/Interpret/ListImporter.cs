namespace WasmBox.Wasm.Interpret {
    public sealed class ListImporter : IImporter {
        /// <summary>
        /// Creates a linking importer.
        /// </summary>
        public ListImporter() {
            ModuleImporters = new();
        }

        public ListImporter(IImporter moduleImporters) {
            ModuleImporters = [moduleImporters];
        }

        public ListImporter(List<IImporter> moduleImporters) {
            ModuleImporters = moduleImporters;
        }

        public List<IImporter> ModuleImporters { get; set; }

        public void RegisterImporter(IImporter importer) {
            ModuleImporters.Add(importer);
        }

        /// <inheritdoc/>
        public FunctionDefinition ImportFunction( ImportedFunction description, FunctionType signature ) {
            foreach ( var importer in ModuleImporters ) {
                var v = importer.ImportFunction( description, signature );
                if ( v != null )
                    return v;
            }
            return null;
        }

        /// <inheritdoc/>
        public Variable ImportGlobal(ImportedGlobal description) {
            foreach ( var importer in ModuleImporters ) {
                var v = importer.ImportGlobal( description );
                if ( v != null )
                    return v;
            }
            return null;
        }

        /// <inheritdoc/>
        public LinearMemory ImportMemory(ImportedMemory description) {
            foreach ( var importer in ModuleImporters ) {
                var v = importer.ImportMemory( description );
                if ( v != null )
                    return v;
            }
            return null;
        }

        /// <inheritdoc/>
        public FunctionTable ImportTable(ImportedTable description) {
            foreach ( var importer in ModuleImporters ) {
                var v = importer.ImportTable( description );
                if ( v != null )
                    return v;
            }
            return null;
        }
    }
}
