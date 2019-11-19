using System;
using Xunit;

namespace AsmResolver.DotNet.Tests
{
    public class AssemblyDefinitionTest
    {
        [Fact]
        public void ReadNameTest()
        {
            var assemblyDef = AssemblyDefinition.FromBytes(Properties.Resources.HelloWorld);
            Assert.Equal("HelloWorld", assemblyDef.Name);
        }

        [Fact]
        public void ReadVersion()
        {
            var assemblyDef = AssemblyDefinition.FromBytes(Properties.Resources.HelloWorld);
            Assert.Equal(new Version(1,0,0,0), assemblyDef.Version);
        }

        [Fact]
        public void ReadSingleModule()
        {
            var assemblyDef = AssemblyDefinition.FromBytes(Properties.Resources.HelloWorld);
            Assert.Single(assemblyDef.Modules);
            Assert.NotNull(assemblyDef.ManifestModule);
            Assert.Equal(new[] {assemblyDef.ManifestModule}, assemblyDef.Modules);
            Assert.Same(assemblyDef, assemblyDef.ManifestModule.Assembly);
        }
    }
}