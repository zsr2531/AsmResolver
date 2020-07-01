using System.Collections.Generic;

namespace AsmResolver.DotNet.Refactoring
{
    public sealed class Workspace
    {
        public ICollection<ModuleDefinition> Modules
        {
            get;
            set;
        } = new List<ModuleDefinition>();
    }
}