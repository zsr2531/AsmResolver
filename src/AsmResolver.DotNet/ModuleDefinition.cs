using System;
using System.Collections.Generic;
using System.Threading;
using AsmResolver.DotNet.Blob;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Serialized;
using AsmResolver.Lazy;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.File;

namespace AsmResolver.DotNet
{
    /// <summary>
    /// Represents a single module in a .NET assembly. A module definition is the root object of any .NET module and
    /// defines types, as well as any resources and referenced assemblies. 
    /// </summary>
    public class ModuleDefinition : IResolutionScope, IOwnedCollectionElement<AssemblyDefinition>
    {
        /// <summary>
        /// Reads a .NET module from the provided input buffer.
        /// </summary>
        /// <param name="buffer">The raw contents of the executable file to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static ModuleDefinition FromBytes(byte[] buffer) => FromImage(PEImage.FromBytes(buffer));
        
        /// <summary>
        /// Reads a .NET module from the provided input file.
        /// </summary>
        /// <param name="filePath">The file path to the input executable to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static ModuleDefinition FromFile(string filePath) => FromImage(PEImage.FromFile(filePath));

        /// <summary>
        /// Reads a .NET module from the provided input file.
        /// </summary>
        /// <param name="file">The portable executable file to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static ModuleDefinition FromFile(PEFile file) => FromImage(PEImage.FromFile(file));

        /// <summary>
        /// Reads a .NET module from an input stream.
        /// </summary>
        /// <param name="reader">The input stream pointing at the beginning of the executable to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static ModuleDefinition FromReader(IBinaryStreamReader reader) => FromImage(PEImage.FromReader(reader));
        
        /// <summary>
        /// Initializes a .NET module from a PE image.
        /// </summary>
        /// <param name="peImage">The image containing the .NET metadata.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public static ModuleDefinition FromImage(IPEImage peImage)
        {
            if (peImage.DotNetDirectory == null)
                throw new BadImageFormatException("Input PE image does not contain a .NET directory.");
            if (peImage.DotNetDirectory.Metadata == null)
                throw new BadImageFormatException("Input PE image does not contain a .NET metadata directory.");
            return FromMetadata(peImage.DotNetDirectory.Metadata);
        }

        /// <summary>
        /// Initializes a .NET module from a .NET metadata directory.
        /// </summary>
        /// <param name="metadata">The object providing access to the underlying metadata streams.</param>
        /// <returns>The module.</returns>
        public static ModuleDefinition FromMetadata(IMetadata metadata)
        {
            var stream = metadata.GetStream<TablesStream>();
            var moduleTable = stream.GetTable<ModuleDefinitionRow>();
            var module = new SerializedModuleDefinition(metadata, new MetadataToken(TableIndex.Module, 1), moduleTable[0]);

            var assemblyTable = stream.GetTable<AssemblyDefinitionRow>();
            if (assemblyTable.Count > 0)
            {
                var assembly = new SerializedAssemblyDefinition(metadata,
                    new MetadataToken(TableIndex.Assembly, 1), 
                    assemblyTable[0], module);
                module.Assembly = assembly;
            }
            return module;
        }

        private readonly LazyVariable<string> _name;
        private readonly LazyVariable<Guid> _mvid;
        private readonly LazyVariable<Guid> _encId;
        private readonly LazyVariable<Guid> _encBaseId;
        private IList<TypeDefinition> _topLevelTypes;
        private IList<AssemblyReference> _assemblyReferences;

        /// <summary>
        /// Initializes a new empty module with the provided metadata token.
        /// </summary>
        /// <param name="token">The metadata token.</param>
        protected ModuleDefinition(MetadataToken token)
        {
            MetadataToken = token;
            _name = new LazyVariable<string>(GetName);
            _mvid = new LazyVariable<Guid>(GetMvid);
            _encId = new LazyVariable<Guid>(GetEncId);
            _encBaseId = new LazyVariable<Guid>(GetEncBaseId);
        }

        /// <summary>
        /// Defines a new .NET module that references mscorlib version 4.0.0.0.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        public ModuleDefinition(string name)
            : this(new MetadataToken(TableIndex.Module, 0))
        {
            Name = name;
            CorLibTypeFactory = CorLibTypeFactory.CreateMscorlib40TypeFactory();
            AssemblyReferences.Add((AssemblyReference) CorLibTypeFactory.CorLibScope);
        }

        /// <summary>
        /// Defines a new .NET module.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="corLib">The reference to the common object runtime (COR) library that this module will use.</param>
        public ModuleDefinition(string name, AssemblyReference corLib)
            : this(new MetadataToken(TableIndex.Module, 0))
        {
            Name = name;
            CorLibTypeFactory = new CorLibTypeFactory(corLib);
            AssemblyReferences.Add(corLib);
        }

        /// <inheritdoc />
        public MetadataToken MetadataToken
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the parent assembly that defines this module.
        /// </summary>
        public AssemblyDefinition Assembly
        {
            get;
            internal set;
        }

        /// <inheritdoc />
        AssemblyDefinition IOwnedCollectionElement<AssemblyDefinition>.Owner
        {
            get => Assembly;
            set => Assembly = value;
        }
        
        /// <inheritdoc />
        ModuleDefinition IModuleProvider.Module => this;

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the Name column in the module definition table. 
        /// </remarks>
        public string Name
        {
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// Gets or sets the generation number of the module.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value is reserved and should be set to zero.
        /// </para>
        /// <para> 
        /// This property corresponds to the Generation column in the module definition table.
        /// </para>
        /// </remarks>
        public ushort Generation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the unique identifier to distinguish between two versions
        /// of the same module.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the MVID column in the module definition table. 
        /// </remarks>
        public Guid Mvid
        {
            get => _mvid.Value;
            set => _mvid.Value = value;
        }

        /// <summary>
        /// Gets or sets the unique identifier to distinguish between two edit-and-continue generations.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the EncId column in the module definition table. 
        /// </remarks>
        public Guid EncId
        {
            get => _encId.Value;
            set => _encId.Value = value;
        }

        /// <summary>
        /// Gets or sets the base identifier of an edit-and-continue generation.
        /// </summary>
        /// <remarks>
        /// This property corresponds to the EncBaseId column in the module definition table. 
        /// </remarks>
        public Guid EncBaseId
        {
            get => _encBaseId.Value;
            set => _encBaseId.Value = value;
        }

        /// <summary>
        /// Gets a collection of top-level (not nested) types defined in the module. 
        /// </summary>
        public IList<TypeDefinition> TopLevelTypes
        {
            get
            {
                if (_topLevelTypes is null)
                    Interlocked.CompareExchange(ref _topLevelTypes, GetTopLevelTypes(), null);
                return _topLevelTypes;
            }
        }

        /// <summary>
        /// Gets a collection of references to .NET assemblies that the module uses. 
        /// </summary>
        public IList<AssemblyReference> AssemblyReferences
        {
            get
            {
                if (_assemblyReferences is null)
                    Interlocked.CompareExchange(ref _assemblyReferences, GetAssemblyReferences(), null);
                return _assemblyReferences;
            }
        }

        /// <summary>
        /// Gets the common object runtime library type factory for this module, containing element type signatures used
        /// in blob signatures. 
        /// </summary>
        public CorLibTypeFactory CorLibTypeFactory
        {
            get;
            protected set;
        }
        
        /// <summary>
        /// Looks up a member by its metadata token.
        /// </summary>
        /// <param name="token">The token of the member to lookup.</param>
        /// <returns>The member.</returns>
        /// <exception cref="InvalidOperationException">
        /// Occurs when the module does not support looking up members by its token.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Occurs when a metadata token indexes a table that cannot be converted to a metadata member.
        /// </exception>
        public virtual IMetadataMember LookupMember(MetadataToken token) =>
            throw new InvalidOperationException("Cannot lookup members by tokens from a non-serialized module.");

        /// <summary>
        /// Obtains the name of the module definition.
        /// </summary>
        /// <returns>The name.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Name"/> property.
        /// </remarks>
        protected virtual string GetName() => null;

        /// <summary>
        /// Obtains the MVID of the module definition.
        /// </summary>
        /// <returns>The MVID.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Mvid"/> property.
        /// </remarks>
        protected virtual Guid GetMvid() => Guid.NewGuid();

        /// <summary>
        /// Obtains the edit-and-continue identifier of the module definition.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="EncId"/> property.
        /// </remarks>
        protected virtual Guid GetEncId() => Guid.Empty;

        /// <summary>
        /// Obtains the edit-and-continue base identifier of the module definition.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="EncBaseId"/> property.
        /// </remarks>
        protected virtual Guid GetEncBaseId() => Guid.Empty;

        /// <summary>
        /// Obtains the list of top-level types the module defines.
        /// </summary>
        /// <returns>The types.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="TopLevelTypes"/> property.
        /// </remarks>
        protected virtual IList<TypeDefinition> GetTopLevelTypes() =>
            new OwnedCollection<ModuleDefinition, TypeDefinition>(this);

        /// <summary>
        /// Obtains the list of references to .NET assemblies that the module uses. 
        /// </summary>
        /// <returns>The references to the assemblies..</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="AssemblyReferences"/> property.
        /// </remarks>
        protected virtual IList<AssemblyReference> GetAssemblyReferences() =>
            new OwnedCollection<ModuleDefinition, AssemblyReference>(this);

        /// <inheritdoc />
        public override string ToString() => Name;

    }
}