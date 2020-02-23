using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AsmResolver.DotNet
{
    /// <summary>
    /// Provides a base implementation of an assembly resolver, that includes a collection of search directories to look
    /// into for probing assemblies.
    /// </summary>
    public abstract class AssemblyResolverBase : IAssemblyResolver
    {
        private static readonly string[] BinaryFileExtensions = {".dll", ".exe"};

        private readonly IDictionary<AssemblyDescriptor, AssemblyDefinition> _cache 
            = new Dictionary<AssemblyDescriptor, AssemblyDefinition>();

        /// <summary>
        /// Gets a collection of custom search directories that are probed upon resolving a reference
        /// to an assembly.
        /// </summary>
        public IList<string> SearchDirectories
        {
            get;
        } = new List<string>();

        public AssemblyDefinition Resolve(AssemblyDescriptor assembly)
        {
            if (_cache.TryGetValue(assembly, out var assemblyDef))
                return assemblyDef;

            assemblyDef = ResolveImpl(assembly);
            if (assemblyDef != null)
                _cache.Add(assembly, assemblyDef);

            return assemblyDef;
        }

        protected abstract AssemblyDefinition ResolveImpl(AssemblyDescriptor assembly);

        /// <summary>
        /// Attempts to read an assembly from its file path.
        /// </summary>
        /// <param name="path">The path to the assembly.</param>
        /// <returns>The assembly.</returns>
        protected virtual AssemblyDefinition LoadAssemblyFromFile(string path)
        {
            return AssemblyDefinition.FromFile(path);
        }

        protected string ProbeSearchDirectories(AssemblyDescriptor assembly)
        {
            foreach (string directory in SearchDirectories)
            {
                string path = ProbeDirectory(assembly, directory);

                if (!string.IsNullOrEmpty(path))
                    return path;
            }

            return null;
        }

        protected static string ProbeDirectory(AssemblyDescriptor assembly, string directory)
        {
            string path = string.IsNullOrEmpty(assembly.Culture)
                ? Path.Combine(directory, assembly.Name)
                : Path.Combine(directory, assembly.Culture, assembly.Name);

            path = ProbeFileFromFilePathWithoutExtension(path)
                   ?? ProbeFileFromFilePathWithoutExtension(Path.Combine(path, assembly.Name));
            return path;
        }

        internal static string ProbeFileFromFilePathWithoutExtension(string baseFilePath)
        {
            return BinaryFileExtensions
                .Select(extension => baseFilePath + extension)
                .FirstOrDefault(File.Exists);
        }
    }
}