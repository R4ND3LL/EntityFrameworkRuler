﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Rules;

#pragma warning disable CS1591

// ReSharper disable InvertIf
// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace EntityFrameworkRuler.Generator.EdmxModel;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class Schema : NotifyPropertyChanged {
    public Schema(ConceptualSchema conceptualSchema, StorageSchema storageSchema) {
        ConceptualSchema = conceptualSchema ?? throw new ArgumentNullException(nameof(conceptualSchema));
        StorageSchema = storageSchema ?? throw new ArgumentNullException(nameof(storageSchema));
    }

    /// <summary> Conceptual namespace.  The namespace that the actual code models are expected to use </summary>
    public string Namespace => ConceptualSchema.Namespace;

    public ConceptualSchema ConceptualSchema { get; }
    public StorageSchema StorageSchema { get; }
    public IList<EntityType> Entities { get; } = new ObservableCollection<EntityType>();
    public IList<AssociationBase> Associations { get; } = new ObservableCollection<AssociationBase>();
    public override string ToString() { return $"Schema: {Namespace}"; }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class EntityType : NotifyPropertyChanged {
    public EntityType(ConceptualEntityType conceptualEntityType, Schema schema) {
        ConceptualEntity = conceptualEntityType ?? throw new ArgumentNullException(nameof(conceptualEntityType));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        schema.Entities.Add(this);
    }

    #region Properties

    public string Namespace => Schema.Namespace;
    public string FullName => $"{Namespace}.{Name}";
    public string SelfName => $"Self.{Name}";

    /// <summary> The conceptual name of the entity </summary>
    public string Name => ConceptualEntity.Name;

    /// <summary> The storage name of the entity </summary>
    public string StorageName => StorageEntity?.Name ?? Name;

    /// <summary> The storage name of the entity with DB Schema </summary>
    public string StorageFullName {
        get {
            var schema = DbSchema;
            var table = StorageEntity?.Name ?? Name;
            if (schema.HasNonWhiteSpace() && table.HasNonWhiteSpace()) return $"{schema}.{table}";
            return table;
        }
    }

    /// <summary> The cleansed storage name (spaces converted to underscore) </summary>
    public string StorageNameIdentifier => StorageName.GenerateCandidateIdentifier(); //.CapitalizeFirst();

    public IList<NavigationProperty> NavigationProperties { get; } =
        new ObservableCollection<NavigationProperty>();

    public IList<EntityProperty> Properties { get; } = new ObservableCollection<EntityProperty>();

    /// <summary> recursively get properties </summary>
    public IEnumerable<EntityProperty> GetProperties(bool? isMapped = null) {
        var props = BaseType != null ? BaseType.GetProperties().Concat(Properties) : Properties;
        if (isMapped.HasValue) props = props.Where(o => o.IsMapped == isMapped.Value);
        return props;
    }

    /// <summary> recursively get properties </summary>
    public IEnumerable<NavigationProperty> GetNavigations() {
        return BaseType != null ? BaseType.GetNavigations().Concat(NavigationProperties) : NavigationProperties;
    }

    /// <summary> recursively get all navigations and properties </summary>
    public IEnumerable<EntityPropertyBase> AllPropertiesAndNavigations =>
        GetProperties().Cast<EntityPropertyBase>().Union(GetNavigations());

    public IList<EndRole> EndRoles { get; } = new ObservableCollection<EndRole>();
    public Schema Schema { get; }
    public StorageEntityType StorageEntity { get; set; }
    public ConceptualEntityType ConceptualEntity { get; set; }
    public IList<MappingFragment> MappingFragments { get; set; }
    public string DbSchema => StorageEntitySet?.Schema ?? StorageEntitySet?.Schema1;
    public EntitySetMapping EntitySetMapping { get; set; }
    public string EntitySetName => EntitySetMapping?.Name;
    public string[] StoreEntitySetNames { get; set; }
    public StorageEntityContainer StorageContainer { get; set; }
    public StorageEntitySet StorageEntitySet { get; set; }
    public IList<ConceptualAssociation> Associations { get; set; }
    public IList<StorageAssociation> StorageAssociations { get; set; }
    public IList<EntityProperty> ConceptualKey { get; } = new List<EntityProperty>();
    public IList<EntityProperty> StorageKey { get; } = new List<EntityProperty>();
    public bool IsAbstract => ConceptualEntity?.Abstract == true;

    private EntityType baseType;

    /// <summary> base class of this entity is another entity type </summary>
    public EntityType BaseType {
        get => baseType;
        set {
            if (baseType == value) return;
            var old = baseType;
            baseType = value;
            OnBaseTypeChanged(old, value);
        }
    }

    private void OnBaseTypeChanged(EntityType old, EntityType n) {
        old?.DerivedTypes.Remove(this);
        n?.DerivedTypes.Add(this);
    }

    /// <summary> entity types that derive from this entity class </summary>
    public HashSet<EntityType> DerivedTypes { get; } = new();

    /// <summary> the discriminator column mapping condition used for a TPH inheritance strategy </summary>
    public MappingCondition Discriminator => MappingFragments.Select(mf => mf.Condition).FirstOrDefault(c => c != null);

    /// <summary> the discriminator property mappings used for a TPH inheritance strategy </summary>
    public List<(EntityProperty Property, MappingCondition Condition, EntityType ToEntity)> DiscriminatorPropertyMappings { get; } = new();

    /// <summary> The inheritance mapping strategy.  TPH, TPT, TPC </summary>
    public string RelationalMappingStrategy { get; set; }

    /// <summary> get the inner most base type, or this if no base type exists </summary>
    public EntityType GetHierarchyRoot() {
        var b = BaseType;
        while (b?.BaseType != null) b = b.BaseType;
        return b ?? this;
    }

    /// <summary> recursively get all entity types that derive from this entity class </summary>
    public IEnumerable<EntityType> GetAllDerivedTypes(bool includeThis = false) {
        if (includeThis) yield return this;
        foreach (var derivedType in DerivedTypes) {
            yield return derivedType;
            foreach (var grandChild in derivedType.GetAllDerivedTypes()) yield return grandChild;
        }
    }

    #endregion


    /// <summary>
    ///     Gets all foreign keys that target a given entity type (i.e. foreign keys where the given entity type
    ///     or a base type is the principal).
    /// </summary>
    /// <returns>The foreign keys that reference the given entity type or a base type.</returns>
    public IEnumerable<ReferentialConstraint> GetReferencingForeignKeys() {
        foreach (var endRole in EndRoles) {
            if (endRole.Association is FkAssociation fkAssociation && fkAssociation?.ReferentialConstraint != null)
                if (fkAssociation.ReferentialConstraint.PrincipalEntity == this) {
                    yield return fkAssociation.ReferentialConstraint;
                }
        }
    }

    #region naming cache

    /// <summary> internal use only to cache the expected EF Core identifier </summary>
    private NamingCache<string> expectedEfCoreName;

    internal string GetExpectedEfCoreName(IRulerNamingService namingService) {
        return expectedEfCoreName.GetValue(namingService);
    }

    internal string SetExpectedEfCoreName(IRulerNamingService namingService, string value) {
        expectedEfCoreName = new(namingService, value);
        return value;
    }

    private NamingCache<List<string>> existingIdentifiers;

    private List<string> GetExistingIdentifiersInternal(IRulerNamingService namingService) {
        return existingIdentifiers.GetValue(namingService);
    }

    private List<string> SetExistingIdentifiers(IRulerNamingService namingService, List<string> value) {
        existingIdentifiers = new(namingService, value);
        return value;
    }

    internal List<string> GetExistingIdentifiers(IRulerNamingService namingService, NavigationProperty skip) {
        var identifiers = GetExistingIdentifiersInternal(namingService);
        if (identifiers != null) return identifiers;
        identifiers = new();
        identifiers.AddRange(Properties.Where(o => o.IsMapped).Select(o => namingService.GetExpectedPropertyName(o, this)));
        identifiers.AddRange(NavigationProperties.Where(o => o != skip).Select(o => o.ConceptualName));
        SetExistingIdentifiers(namingService, identifiers);
        return identifiers;
    }

    #endregion


    public override string ToString() { return BaseType != null ? $"Entity: {Name}: {BaseType.Name}" : $"Entity: {Name}"; }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class Function : NotifyPropertyChanged {
    public Function(StorageFunction storageFunction) {
        this.StorageFunction = storageFunction;
    }

    public StorageFunction StorageFunction { get; }
    public FunctionImportMapping ImportMapping { get; set; }
    public ConceptualFunctionImport Import { get; set; }
    public string Name => ImportMapping?.FunctionImportName ?? StorageFunction.Name;
    public string DbName => StorageFunction.StoreFunctionName.CoalesceWhiteSpace(StorageFunction.Name);
    public bool IsMapped => ImportMapping != null && Import != null;
    public string Schema => StorageFunction.Schema;
}

[DebuggerDisplay("{value}")]
internal readonly struct NamingCache<T> {
    public NamingCache(IRulerNamingService owner, T value) {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.value = value;
    }

    public IRulerNamingService Owner { get; }
    private readonly T value;

    public T GetValue(IRulerNamingService current) {
        return ReferenceEquals(current, Owner) ? value : default;
    }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public abstract class EntityPropertyBase : NotifyPropertyChanged, IEntityProperty {
    protected EntityPropertyBase(EntityType e, IPropertyRef conceptualProperty) {
        ConceptualProperty = conceptualProperty;
        Entity = e;
    }

    /// <summary> The conceptual name of the property </summary>
    public virtual string ConceptualName => ConceptualProperty?.Name;

    /// <summary> true if this property is mapped in the EDMX </summary>
    public bool IsMapped => ConceptualProperty?.Name != null;

    public EntityType Entity { get; }
    public IPropertyRef ConceptualProperty { get; }
    public string EntityName => Entity?.Name;

    #region naming cache

    /// <summary> internal use only to cache the expected EF Core identifier </summary>
    private NamingCache<string> expectedEfCoreName;

    string IEntityProperty.GetExpectedEfCoreName(IRulerNamingService namingService) {
        return expectedEfCoreName.GetValue(namingService);
    }

    string IEntityProperty.SetExpectedEfCoreName(IRulerNamingService namingService, string value) {
        expectedEfCoreName = new(namingService, value);
        return value;
    }

    public abstract string GetName(bool conceptualPreferred = true);

    #endregion

    public override string ToString() { return $"Prop: {GetName()}"; }
}

public interface IEntityProperty {
    string GetExpectedEfCoreName(IRulerNamingService namingService);
    string SetExpectedEfCoreName(IRulerNamingService namingService, string value);
    string GetName(bool conceptualPreferred = true);
    bool IsMapped { get; }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class EntityProperty : EntityPropertyBase {
    public EntityProperty(EntityType entity, ConceptualProperty conceptualProperty, StorageProperty storageProperty) : base(entity,
        conceptualProperty) {
        StorageProperty = storageProperty;
    }

    public override string ConceptualName => ConceptualProperty?.Name;
    public string ColumnName => Mapping?.ColumnName ?? StorageProperty?.Name;

    public override string GetName(bool conceptualPreferred = true) =>
        conceptualPreferred ? ConceptualName ?? ColumnName : ColumnName ?? ConceptualName;

    /// <summary> the database type name </summary>
    public string DbTypeName => StorageProperty?.Type;

    /// <summary> the conceptual (code) type name </summary>
    public string ClrTypeName => ConceptualProperty?.Type;

    private EnumType enumType;
    public EnumType EnumType { get => enumType; set => SetProperty(ref enumType, value, OnEnumTypeChanged); }
    private void OnEnumTypeChanged() { }

    public bool Nullable => StorageProperty?.Nullable?.StartsWithIgnoreCase("t") == true;
    public bool IsIdentity => StorageProperty?.StoreGeneratedPattern == "Identity";

    public StorageProperty StorageProperty { get; set; }
    public ScalarPropertyMapping Mapping { get; set; }
    public new ConceptualProperty ConceptualProperty => base.ConceptualProperty as ConceptualProperty;
    public bool IsConceptualKey { get; set; }
    public bool IsStorageKey { get; set; }

    public override string ToString() { return $"Prop: {GetName()}"; }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class NavigationProperty : EntityPropertyBase {
    public NavigationProperty(EntityType conceptualEntity, ConceptualNavigationProperty conceptualProperty)
        : base(conceptualEntity, conceptualProperty) {
        if (conceptualProperty == null) throw new ArgumentNullException(nameof(conceptualProperty));
    }

    public string Relationship => ConceptualNavigationProperty?.Relationship;
    public string FromRoleName => ConceptualNavigationProperty?.FromRole;
    public string ToRoleName => ConceptualNavigationProperty?.ToRole;

    private AssociationBase association;

    public AssociationBase Association {
        get => association;
        set => SetProperty(ref association, value, OnAssociationChanged);
    }

    private void OnAssociationChanged(AssociationBase o) {
        if (o?.Navigations.Contains(this) == true) o.Navigations.Remove(this);
        if (association?.Navigations.Contains(this) == false) association.Navigations.Add(this);
    }

    public bool IsSelfReference => Association?.IsSelfReference == true;
    public Multiplicity Multiplicity => ToRole?.Multiplicity ?? Multiplicity.Unknown;

    /// <summary> this end role determines the the property value type or return type </summary>
    public EndRole ToRole { get; set; } // Association?.EndRoles.FirstOrDefault(o => o.Role == ToRoleName);

    /// <summary> this role is the source data type </summary>
    public EndRole FromRole { get; set; } // Association?.EndRoles.FirstOrDefault(o => o.Role == FromRoleName);

    public ConceptualNavigationProperty ConceptualNavigationProperty =>
        (ConceptualNavigationProperty)ConceptualProperty;

    public ConceptualAssociation ConceptualAssociation { get; set; }
    public StorageAssociation StorageAssociation { get; set; }

    /// <summary> true if this is the dependent end of the association. </summary>
    public bool IsDependentEnd { get; set; }

    /// <summary> true if this is the principal end of the association. </summary>
    public bool IsPrincipalEnd => !IsDependentEnd;

    /// <summary> the underlying foreign key properties that establish this association on this side of the relationship (i.e. this entity) </summary>
    public EntityProperty[] FkProperties { get; set; }

    /// <summary> The navigation property at the other end of this relationship (on the other entity). </summary>
    public NavigationProperty InverseNavigation { get; set; }

    /// <summary> The EntityType at the other end of this relationship. </summary>
    public EntityType InverseEntity => ToRole?.Entity;

    public override string GetName(bool conceptualPreferred = true) => ConceptualName;

    public override string ToString() { return $"NProp: {ConceptualName}"; }
}

/// <summary>
/// Association that has no conceptual entity foreign key such as many-to-many associations where the junction table
/// has no conceptual representation, and therefore, no entity level dependent fields.
/// Further, the entities on both ends can both be considered principals.  There is no principal/dependent relationship here.
/// </summary>
public sealed class DesignAssociation : AssociationBase {
    public DesignAssociation(ConceptualAssociation conceptualAssociation, IList<EndRole> roles,
        NavigationProperty fromNavigation, NavigationProperty toNavigation) : base(
        conceptualAssociation, roles, fromNavigation, toNavigation) { }


    // public NavigationProperty FromNavigation { get; }
    // public EntityType FromEntity => FromNavigation?.Entity;
    // public NavigationProperty ToNavigation { get; }
    // public EntityType ToEntity => ToNavigation?.Entity;

    public override string ToString() {
        return $"Design Association: {Name}";
    }
}

/// <summary> Association that is based on a foreign key (majority of all associations). </summary>
public sealed class FkAssociation : AssociationBase {
    public FkAssociation(ConceptualAssociation conceptualAssociation, IList<EndRole> roles,
        NavigationProperty fromNavigation, NavigationProperty toNavigation, ReferentialConstraint constraint) : base(
        conceptualAssociation, roles, fromNavigation, toNavigation) {
        ReferentialConstraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
        constraint.Association = this;

        foreach (var navigation in Navigations) {
            var isPrincipal = navigation.FromRoleName == constraint.PrincipalRole;
            var isDependent = navigation.FromRoleName == constraint.DependentRole;
            Debug.Assert(isPrincipal ^ isDependent);
            Debug.Assert(navigation.IsDependentEnd == isDependent);
            navigation.IsDependentEnd = isDependent;
            navigation.FkProperties = (isDependent ? constraint.DependentProperties : constraint.PrincipalProperties) ??
                                      Array.Empty<EntityProperty>();
        }
    }

    public override string Name => ReferentialConstraint?.StorageAssociation?.Name ?? base.Name;

    public ReferentialConstraint ReferentialConstraint { get; }
    public override string ToString() { return $"FK Association: {Name}"; }
}

/// <summary> Represents an association (or relation) between entities.  Note, this instance is shared between the two navigation properties. </summary>
public abstract class AssociationBase : NotifyPropertyChanged {
    protected AssociationBase(ConceptualAssociation conceptualAssociation, IList<EndRole> roles,
        params NavigationProperty[] navigations) {
        ConceptualAssociation = conceptualAssociation ?? throw new ArgumentNullException(nameof(conceptualAssociation));
        if (navigations is not { Length: 2 })
            throw new ArgumentException("Association should have 2 navigations", nameof(navigations));
        foreach (var navigation in navigations) {
            if (navigation == null) continue;
            Debug.Assert(navigation.Association == null);
            navigation.Association = this;
            Debug.Assert(Navigations.Contains(navigation));
            navigation.ToRole = roles.Single(o => o.Role == navigation.ToRoleName);
            navigation.FromRole = roles.Single(o => o.Role == navigation.FromRoleName);
            navigation.InverseNavigation = navigations.Single(o => o != navigation);
            Debug.Assert(navigation.InverseNavigation == null || navigation.InverseNavigation.Entity == navigation.ToRole.Entity);
        }

        foreach (var navigation in Navigations) {
            var constraint = conceptualAssociation?.ReferentialConstraint;
            var isMany = navigation.Multiplicity == Multiplicity.Many;
            var isManyToMany = isMany && navigation.InverseNavigation?.Multiplicity == Multiplicity.Many;
            var isPrincipal = isMany || navigation.FromRoleName == constraint?.Principal?.Role;
            var isDependent = !isManyToMany && navigation.FromRoleName == constraint?.Dependent?.Role;

            if (!(isPrincipal ^ isDependent)) {
                // inconclusive.  check multiplicity for a clue
                var inverseIsMany = navigation.InverseNavigation?.Multiplicity == Multiplicity.Many;
                var inverseIsOne = navigation.InverseNavigation?.Multiplicity.In(Multiplicity.One, Multiplicity.ZeroOne) == true;
                var thisIsOne = navigation.Multiplicity.In(Multiplicity.One, Multiplicity.ZeroOne);

                if (isMany && inverseIsOne) {
                    // collections are always principal
                    isDependent = false;
                    isPrincipal = true;
                } else if (thisIsOne && inverseIsMany) {
                    // collection inverse is always dependent
                    isDependent = true;
                    isPrincipal = false;
                }
            }

            Debug.Assert(isPrincipal ^ isDependent);
            navigation.IsDependentEnd = isDependent;
        }

        Schema = Navigations.FirstOrDefault(o => o.Entity?.Schema != null)?.Entity.Schema ??
                 throw new ArgumentNullException(nameof(navigations));
        Schema.Associations.Add(this);
        if (roles?.Count > 0) {
            EndRoles = roles;
            foreach (var o in roles) {
                Debug.Assert(o.Association == null);
                o.Association = this;
            }
        } else
            throw new InvalidDataException("new association has no roles: " + conceptualAssociation);
    }

    /// <summary> navigations involved in this association </summary>
    public IList<NavigationProperty> Navigations { get; } = new List<NavigationProperty>();

    /// <summary> Gets this is the relationship name. </summary>
    public virtual string Name => ConceptualAssociation.Name;

    public ConceptualAssociation ConceptualAssociation { get; }
    public IList<EndRole> EndRoles { get; }
    public Schema Schema { get; }

    public bool IsSelfReference => EndRoles?.Count > 0 && EndRoles[0].Entity != null &&
                                   EndRoles.All(o => o.Entity == EndRoles[0].Entity);

    public override string ToString() { return $"Association: {Name}"; }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class EnumTypeMember : NotifyPropertyChanged {
    public EnumTypeMember(ConceptualEnumMember conceptualEnumMember) {
        ConceptualEnumMember = conceptualEnumMember;
        value = long.TryParse(conceptualEnumMember.Value, out var l) ? l : -1;
    }

    public ConceptualEnumMember ConceptualEnumMember { get; }

    /// <summary> this is the member name </summary>
    public string Name => ConceptualEnumMember.Name;

    private long value;
    public long Value { get => value; set => SetProperty(ref this.value, value); }

    public override string ToString() { return $"Enum Member: {Name} ({Value})"; }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class EnumType : NotifyPropertyChanged {
    public EnumType(ConceptualEnumType conceptualEnumType, Schema schema) {
        ConceptualEnumType = conceptualEnumType;
        Schema = schema;
        Members = conceptualEnumType.Members?.Select(o => new EnumTypeMember(o)).ToArray() ??
                  Array.Empty<EnumTypeMember>();
    }

    public ConceptualEnumType ConceptualEnumType { get; }

    /// <summary> this is the enum name, no namespace </summary>
    public string Name => ConceptualEnumType.Name;

    public string FullName => $"{Schema.Namespace}.{ConceptualEnumType.Name}";

    public EnumTypeMember[] Members { get; }

    public string UnderlyingType => ConceptualEnumType.UnderlyingType;
    public bool IsFlags => ConceptualEnumType.IsFlags?.ToLower().StartsWith("t") == true;
    public string ExternalTypeName => ConceptualEnumType.ExternalTypeName;
    public Schema Schema { get; }

    /// <summary> properties that use this enum </summary>
    public IList<EntityProperty> Properties { get; } = new ObservableCollection<EntityProperty>();

    public override string ToString() { return $"EnumType: {Name} (Ext: {ExternalTypeName})"; }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class EndRole : NotifyPropertyChanged {
    public EndRole(ConceptualAssociation conceptualAssociation, ConceptualEnd conceptualEnd, EntityType entityType) {
        ConceptualAssociation = conceptualAssociation;
        ConceptualEnd = conceptualEnd ?? throw new ArgumentNullException(nameof(conceptualEnd));
        Entity = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Multiplicity = conceptualEnd.Multiplicity.ParseMultiplicity();
        entityType.EndRoles.Add(this);
    }

    public string Role => ConceptualEnd.Role;
    public string AssociationName => ConceptualAssociation.Name;
    public ConceptualAssociation ConceptualAssociation { get; }
    public ConceptualEnd ConceptualEnd { get; }
    public EntityType Entity { get; }
    public Multiplicity Multiplicity { get; }

    private AssociationBase associations;

    public AssociationBase Association {
        get => associations;
        set => SetProperty(ref associations, value);
    }

    public override string ToString() { return $"End: {Role}"; }
}

/// <summary>
/// Represents a referential constraint. That is, a entity relation where dependent scalar properties are used to draw the link.
/// AKA Foreign key.
/// </summary>
public sealed class ReferentialConstraint : NotifyPropertyChanged {
    public ReferentialConstraint(
        List<IReferentialConstraint> constraints,
        EntityProperty[] principalProps,
        EntityProperty[] depProps, EntityType principal, EntityType dependent) {
        ConceptualReferentialConstraint = constraints.OfType<ConceptualReferentialConstraint>().FirstOrDefault();
        StorageReferentialConstraint = constraints.OfType<StorageReferentialConstraint>().FirstOrDefault();
#if DEBUG
        if (principalProps.IsNullOrEmpty()) throw new ArgumentNullException(nameof(principalProps));
        if (depProps.IsNullOrEmpty()) throw new ArgumentNullException(nameof(depProps));
#endif

        PrincipalProperties = principalProps?.ToArray() ?? Array.Empty<EntityProperty>();
        DependentProperties = depProps?.ToArray() ?? Array.Empty<EntityProperty>();
        PrincipalEntity = principal; // PrincipalProperties.FirstOrDefault()?.Entity;
        DependentEntity = dependent; //DependentProperties.FirstOrDefault()?.Entity;
    }

    public ConceptualReferentialConstraint ConceptualReferentialConstraint { get; }
    public StorageReferentialConstraint StorageReferentialConstraint { get; }
    public StorageAssociation StorageAssociation { get; set; }


    public string PrincipalRole => ConceptualReferentialConstraint.Principal.Role;
    public EntityProperty[] PrincipalProperties { get; }

    public string DependentRole => ConceptualReferentialConstraint.Dependent.Role;
    public EntityProperty[] DependentProperties { get; }


    private FkAssociation association;

    public FkAssociation Association {
        get => association;
        set => SetProperty(ref association, value, OnAssociationChanged);
    }

    private void OnAssociationChanged(FkAssociation o) {
        // if (o?.ReferentialConstraint.Contains(this) == true) o.ReferentialConstraint.Remove(this);
        // if (association?.ReferentialConstraint.Contains(this) == false) association.ReferentialConstraint.Add(this);
    }

    public EntityType PrincipalEntity { get; }
    public EntityType DependentEntity { get; }

    /// <summary>
    /// Gets a value indicating whether the values assigned to the foreign key properties are unique.
    /// This is on the EFC foreign key.  As best I understand it, this wants to know if the dependent properties
    /// form a unique key on the dependent entity?
    /// </summary>
    public bool IsUnique => DependentProperties.Length == DependentEntity.StorageKey.Count &&
                            DependentProperties.All(o => o.IsStorageKey);


    public bool IsSelfReferencing() => PrincipalEntity != null && PrincipalEntity == DependentEntity;

    public override string ToString() { return $"Ref: {PrincipalRole}"; }
}