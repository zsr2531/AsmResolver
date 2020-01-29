using System.Collections.Generic;
using System.Threading;
using AsmResolver.DotNet.Collections;
using AsmResolver.Lazy;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AsmResolver.DotNet
{
    /// <summary>
    /// Represents a type parameter that a generic method or type in a .NET module defines.
    /// </summary>
    public class GenericParameter : INameProvider, IHasCustomAttribute, IOwnedCollectionElement<IHasGenericParameters>
    {
        private readonly LazyVariable<string> _name;
        private readonly LazyVariable<IHasGenericParameters> _owner;
        private IList<CustomAttribute> _customAttributes;

        /// <summary>
        /// Initializes a new empty generic parameter.
        /// </summary>
        /// <param name="token">The token of the generic parameter.</param>
        protected GenericParameter(MetadataToken token)
        {
            MetadataToken = token;
            _name = new LazyVariable<string>(GetName);
            _owner = new LazyVariable<IHasGenericParameters>(GetOwner);
        }

        /// <summary>
        /// Creates a new generic parameter.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        public GenericParameter(string name)
            : this(new MetadataToken(TableIndex.GenericParam, 0))
        {
            Name = name;
        }

        /// <summary>
        /// Creates a new generic parameter.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="attributes">Additional attributes to assign to the parameter.</param>
        public GenericParameter(string name, GenericParameterAttributes attributes)
            : this(new MetadataToken(TableIndex.GenericParam, 0))
        {
            Name = name;
            Attributes = attributes;
        }

        /// <inheritdoc />
        public MetadataToken MetadataToken
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the member that defines this generic parameter.
        /// </summary>
        public IHasGenericParameters Owner
        {
            get => _owner.Value;
            internal set => _owner.Value = value;
        }

        IHasGenericParameters IOwnedCollectionElement<IHasGenericParameters>.Owner
        {
            get => Owner;
            set => Owner = value;
        }

        /// <inheritdoc />
        public string Name
        {
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// Gets or sets additional attributes assigned to this generic parameter. 
        /// </summary>
        public GenericParameterAttributes Attributes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the index of this parameter within the list of generic parameters that the owner defines.
        /// </summary>
        public ushort Number
        {
            get;
            internal set;
        }

        /// <inheritdoc />
        public IList<CustomAttribute> CustomAttributes
        {
            get{
                if (_customAttributes is null)
                    Interlocked.CompareExchange(ref _customAttributes, GetCustomAttributes(), null);
                return _customAttributes;
                
            }
        }
        
        /// <summary>
        /// Obtains the name of the generic parameter.
        /// </summary>
        /// <returns>The name.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Name"/> property.
        /// </remarks>
        protected virtual string GetName() => null;
        
        /// <summary>
        /// Obtains the owner of the generic parameter.
        /// </summary>
        /// <returns>The owner</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Owner"/> property.
        /// </remarks>
        protected virtual IHasGenericParameters GetOwner() => null;

        /// <summary>
        /// Obtains the list of custom attributes assigned to the member.
        /// </summary>
        /// <returns>The attributes</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="CustomAttributes"/> property.
        /// </remarks>
        protected virtual IList<CustomAttribute> GetCustomAttributes() => 
            new OwnedCollection<IHasCustomAttribute, CustomAttribute>(this);

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}