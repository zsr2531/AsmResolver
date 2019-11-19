using System.Collections.Generic;
using System.Threading;
using AsmResolver.DotNet.Collections;
using AsmResolver.Lazy;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AsmResolver.DotNet
{
    /// <summary>
    /// Represents a type (a class, interface or structure) defined in a .NET module.
    /// </summary>
    public class TypeDefinition : ITypeDefOrRef, 
        IOwnedCollectionElement<ModuleDefinition>,
        IOwnedCollectionElement<TypeDefinition>
    {
        private readonly LazyVariable<string> _namespace;
        private readonly LazyVariable<string> _name;
        private readonly LazyVariable<ITypeDefOrRef> _baseType;
        private readonly LazyVariable<TypeDefinition> _declaringType;
        private IList<TypeDefinition> _nestedTypes;
        private string _fullName;
        private ModuleDefinition _module;

        /// <summary>
        /// Initializes a new type definition.
        /// </summary>
        /// <param name="token">The token of the type definition.</param>
        protected TypeDefinition(MetadataToken token)
        {
            MetadataToken = token;
            _namespace = new LazyVariable<string>(GetNamespace);
            _name = new LazyVariable<string>(GetName);
            _baseType = new LazyVariable<ITypeDefOrRef>(GetBaseType);
            _declaringType = new LazyVariable<TypeDefinition>(GetDeclaringType);
        }

        /// <summary>
        /// Creates a new type definition.
        /// </summary>
        /// <param name="ns">The namespace the type resides in.</param>
        /// <param name="name">The name of the type.</param>
        /// <param name="attributes">The attributes associated to the type.</param>
        public TypeDefinition(string ns, string name, TypeAttributes attributes)
            : this(ns, name, attributes, null)
        {
        }

        /// <summary>
        /// Creates a new type definition.
        /// </summary>
        /// <param name="ns">The namespace the type resides in.</param>
        /// <param name="name">The name of the type.</param>
        /// <param name="attributes">The attributes associated to the type.</param>
        /// <param name="baseType">The super class that this type extends.</param>
        public TypeDefinition(string ns, string name, TypeAttributes attributes, ITypeDefOrRef baseType)
            : this(new MetadataToken(TableIndex.TypeDef, 0))
        {
            Namespace = ns;
            Name = name;
            Attributes = attributes;
            BaseType = baseType;
        }

        /// <inheritdoc />
        public MetadataToken MetadataToken
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the namespace the type resides in.
        /// </summary>
        public string Namespace
        {
            get => _namespace.Value;
            set
            {
                _namespace.Value = value;
                _fullName = null;
            }
        }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        public string Name
        {
            get => _name.Value;
            set
            {
                _name.Value = value;
                _fullName = null;
            }
        }

        /// <summary>
        /// Gets the full name (including namespace or declaring type full name) of the type.
        /// </summary>
        public string FullName => _fullName ?? (_fullName = this.GetFullName());

        /// <summary>
        /// Gets or sets the attributes associated to the type.
        /// </summary>
        public TypeAttributes Attributes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the type is in a public scope or not.
        /// </summary>
        public bool IsNotPublic
        {
            get => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic;
            set => Attributes = value ? Attributes & ~TypeAttributes.VisibilityMask : Attributes;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the type is in a public scope or not.
        /// </summary>
        public bool IsPublic
        {
            get => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;
            set => Attributes = (Attributes & ~TypeAttributes.VisibilityMask)
                                | (value ? TypeAttributes.Public : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the type is nested with public visibility.
        /// </summary>
        /// <remarks>
        /// Updating the value of this property does not automatically make the type nested in another type.
        /// Similarly, adding this type to another enclosing type will not automatically update this property. 
        /// </remarks>
        public bool IsNestedPublic
        {
            get => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;
            set => Attributes = (Attributes & ~TypeAttributes.VisibilityMask)
                                | (value ? TypeAttributes.NestedPublic : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the type is nested with private visibility.
        /// </summary>
        /// <remarks>
        /// Updating the value of this property does not automatically make the type nested in another type.
        /// Similarly, adding this type to another enclosing type will not automatically update this property. 
        /// </remarks>
        public bool IsNestedPrivate
        {
            get => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;
            set => Attributes = (Attributes & ~TypeAttributes.VisibilityMask)
                                | (value ? TypeAttributes.NestedPrivate : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the type is nested with family visibility.
        /// </summary>
        /// <remarks>
        /// Updating the value of this property does not automatically make the type nested in another type.
        /// Similarly, adding this type to another enclosing type will not automatically update this property. 
        /// </remarks>
        public bool IsNestedFamily
        {
            get => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;
            set => Attributes = (Attributes & ~TypeAttributes.VisibilityMask)
                                | (value ? TypeAttributes.NestedFamily : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the type is nested with assembly visibility.
        /// </summary>
        /// <remarks>
        /// Updating the value of this property does not automatically make the type nested in another type.
        /// Similarly, adding this type to another enclosing type will not automatically update this property. 
        /// </remarks>
        public bool IsNestedAssembly
        {
            get => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly;
            set => Attributes = (Attributes & ~TypeAttributes.VisibilityMask)
                                | (value ? TypeAttributes.NestedAssembly : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the type is nested with family and assembly visibility.
        /// </summary>
        /// <remarks>
        /// Updating the value of this property does not automatically make the type nested in another type.
        /// Similarly, adding this type to another enclosing type will not automatically update this property. 
        /// </remarks>
        public bool IsNestedFamilyAndAssembly
        {
            get => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamilyAndAssembly;
            set => Attributes = (Attributes & ~TypeAttributes.VisibilityMask)
                                | (value ? TypeAttributes.NestedFamilyAndAssembly : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the type is nested with family or assembly visibility.
        /// </summary>
        /// <remarks>
        /// Updating the value of this property does not automatically make the type nested in another type.
        /// Similarly, adding this type to another enclosing type will not automatically update this property. 
        /// </remarks>
        public bool IsNestedFamilyOrAssembly
        {
            get => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamilyOrAssembly;
            set => Attributes = (Attributes & ~TypeAttributes.VisibilityMask)
                                | (value ? TypeAttributes.NestedFamilyOrAssembly : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the fields of the type are auto-laid out by the
        /// common language runtime (CLR).
        /// </summary>
        public bool IsAutoLayout
        {
            get => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout;
            set => Attributes = value ? (Attributes & ~TypeAttributes.LayoutMask) : Attributes;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the fields of the type are laid out sequentially.
        /// </summary>
        public bool IsSequentialLayout
        {
            get => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;
            set => Attributes = (Attributes & ~TypeAttributes.LayoutMask)
                                | (value ? TypeAttributes.SequentialLayout : 0);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the fields of the type are laid out explicitly.
        /// </summary>
        public bool IsExplicitLayout
        {
            get => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;
            set => Attributes = (Attributes & ~TypeAttributes.LayoutMask)
                                | (value ? TypeAttributes.ExplicitLayout : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the type is a class.
        /// </summary>
        public bool IsClass
        {
            get => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
            set => Attributes = value ? Attributes & ~TypeAttributes.ClassSemanticsMask : Attributes;
        }
        
        
        /// <summary>
        /// Gets or sets a value indicating whether the type is an interface.
        /// </summary>
        public bool IsInterface
        {
            get => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
            set => Attributes = (Attributes & ~TypeAttributes.ClassSemanticsMask)
                                | (value ? TypeAttributes.Interface : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the type is defined abstract and should be extended before
        /// an object can be instantiated.
        /// </summary>
        public bool IsAbstract
        {
            get => (Attributes & TypeAttributes.Abstract) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.Abstract)
                                | (value ? TypeAttributes.Abstract : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the type is defined sealed and cannot be extended by a sub class.
        /// </summary>
        public bool IsSealed
        {
            get => (Attributes & TypeAttributes.Sealed) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.Sealed)
                                | (value ? TypeAttributes.Sealed : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the type has a special name.
        /// </summary>
        public bool IsSpecialName
        {
            get => (Attributes & TypeAttributes.SpecialName) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.SpecialName)
                                | (value ? TypeAttributes.SpecialName : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the runtime should check the encoding of the name.
        /// </summary>
        public bool IsRuntimeSpecialName
        {
            get => (Attributes & TypeAttributes.Forwarder) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.Forwarder)
                                | (value ? TypeAttributes.Forwarder : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the type is imported.
        /// </summary>
        public bool IsImport
        {
            get => (Attributes & TypeAttributes.Import) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.Import)
                                | (value ? TypeAttributes.Import : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the type is serializable.
        /// </summary>
        public bool IsSerializable
        {
            get => (Attributes & TypeAttributes.Serializable) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.Serializable)
                                | (value ? TypeAttributes.Serializable : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether LPTSTR string instances are interpreted as ANSI strings.
        /// </summary>
        public bool IsAnsiClass
        {
            get => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass;
            set => Attributes = value ? Attributes & ~TypeAttributes.StringFormatMask : Attributes;
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether LPTSTR string instances are interpreted as Unicode strings.
        /// </summary>
        public bool IsUnicodeClass
        {
            get => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass;
            set => Attributes = (Attributes & ~TypeAttributes.StringFormatMask)
                                | (value ? TypeAttributes.UnicodeClass : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether LPTSTR string instances are interpreted automatically by the runtime.
        /// </summary>
        public bool IsAutoClass
        {
            get => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass;
            set => Attributes = (Attributes & ~TypeAttributes.StringFormatMask)
                                | (value ? TypeAttributes.AutoClass : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether LPTSTR string instances are interpreted using a non-standard encoding.
        /// </summary>
        public bool IsCustomFormatClass
        {
            get => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.CustomFormatClass;
            set => Attributes = (Attributes & ~TypeAttributes.StringFormatMask)
                                | (value ? TypeAttributes.CustomFormatClass : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating the runtime should initialize the class before any time before the first
        /// static field access.
        /// </summary>
        public bool IsBeforeFieldInit
        {
            get => (Attributes & TypeAttributes.BeforeFieldInit) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.BeforeFieldInit)
                                | (value ? TypeAttributes.BeforeFieldInit : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating the type is an exported type and forwards the definition to another module.
        /// </summary>
        public bool IsForwarder
        {
            get => (Attributes & TypeAttributes.Forwarder) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.Forwarder)
                                | (value ? TypeAttributes.Forwarder : 0);
        }
        
        /// <summary>
        /// Gets or sets a value indicating the type has additional security attributes associated to it.
        /// </summary>
        public bool HasSecurity
        {
            get => (Attributes & TypeAttributes.HasSecurity) != 0;
            set => Attributes = (Attributes & ~TypeAttributes.HasSecurity)
                                | (value ? TypeAttributes.HasSecurity : 0);
        }
        
        /// <summary>
        /// Gets or sets the super class that this type extends. 
        /// </summary>
        public ITypeDefOrRef BaseType
        {
            get => _baseType.Value;
            set => _baseType.Value = value;
        }

        /// <summary>
        /// Gets the module that defines the type.
        /// </summary>
        public ModuleDefinition Module => DeclaringType != null ? DeclaringType.Module : _module;

        ModuleDefinition IOwnedCollectionElement<ModuleDefinition>.Owner
        {
            get => Module;
            set => _module = value;
        }

        /// <summary>
        /// When this type is nested, gets the enclosing type.
        /// </summary>
        public TypeDefinition DeclaringType
        {
            get => _declaringType.Value;
            private set => _declaringType.Value = value;
        }
        
        ITypeDefOrRef ITypeDefOrRef.DeclaringType => DeclaringType;
        
        ITypeDescriptor IMemberDescriptor.DeclaringType => DeclaringType;
        
        TypeDefinition IOwnedCollectionElement<TypeDefinition>.Owner
        {
            get => DeclaringType;
            set => DeclaringType = value;
        }

        /// <summary>
        /// Gets a collection of nested types that this type defines.
        /// </summary>
        public IList<TypeDefinition> NestedTypes
        {
            get
            {
                if (_nestedTypes is null)
                    Interlocked.CompareExchange(ref _nestedTypes, GetNestedTypes(), null);
                return _nestedTypes;
            }
        }

        IResolutionScope ITypeDescriptor.Scope => Module;

        /// <inheritdoc />
        public bool IsValueType => BaseType.IsTypeOf("System", "ValueType") || IsEnum;

        /// <summary>
        /// Gets a value indicating whether the type defines an enumeration of discrete values.
        /// </summary>
        public bool IsEnum => BaseType.IsTypeOf("System", "Enum");
        
        /// <summary>
        /// Obtains the namespace of the type definition.
        /// </summary>
        /// <returns>The namespace.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Namespace"/> property.
        /// </remarks>
        protected virtual string GetNamespace() => null;

        /// <summary>
        /// Obtains the name of the type definition.
        /// </summary>
        /// <returns>The namespace.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Name"/> property.
        /// </remarks>
        protected virtual string GetName() => null;

        /// <summary>
        /// Obtains the base type of the type definition.
        /// </summary>
        /// <returns>The namespace.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="BaseType"/> property.
        /// </remarks>
        protected virtual ITypeDefOrRef GetBaseType() => null;

        /// <summary>
        /// Obtains the list of nested types that this type defines.
        /// </summary>
        /// <returns>The nested types.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="NestedTypes"/> property.
        /// </remarks>
        protected virtual IList<TypeDefinition> GetNestedTypes() =>
            new OwnedCollection<TypeDefinition, TypeDefinition>(this);

        /// <summary>
        /// Obtains the enclosing class of the type definition if available.
        /// </summary>
        /// <returns>The enclosing type.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="DeclaringType"/> property.
        /// </remarks>
        protected virtual TypeDefinition GetDeclaringType() => null;

        /// <inheritdoc />
        public override string ToString() => FullName;
    }
}