using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EdmxRuler.Extensions;

// ReSharper disable InvertIf
// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace EdmxRuler.Generator.EdmxModel;

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
    public string StorageName => StorageEntity?.Name;

    /// <summary> The cleansed storage name (spaces converted to underscore) </summary>
    public string StorageNameCleansed => StorageName.CleanseSymbolName();

    public IList<NavigationProperty> NavigationProperties { get; } =
        new ObservableCollection<NavigationProperty>();

    public IList<EntityProperty> Properties { get; } = new ObservableCollection<EntityProperty>();

    public IEnumerable<EntityPropertyBase> AllProperties =>
        Properties.OfType<EntityPropertyBase>().Union(NavigationProperties);

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
    public IList<EntityProperty> ConceptualKey { get; } = new List<EntityProperty>();
    public IList<EntityProperty> StorageKey { get; } = new List<EntityProperty>();

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

    private List<string> existingIdentifiers;

    public List<string> GetExistingIdentifiers() {
        if (existingIdentifiers != null) return existingIdentifiers;
        existingIdentifiers = new();
        existingIdentifiers.AddRange(this.Properties.Select(o => o.DbColumnNameCleansed));
        existingIdentifiers.AddRange(this.NavigationProperties.Select(o => o.Name));
        return existingIdentifiers;
    }

    public override string ToString() { return $"Entity: {Name}"; }
}

public class EntityPropertyBase : NotifyPropertyChanged {
    public EntityPropertyBase(EntityType e, IConceptualProperty conceptualProperty) {
        ConceptualProperty = conceptualProperty;
        Entity = e;
    }

    /// <summary> The conceptual name of the property </summary>
    public string Name => ConceptualProperty.Name;

    public EntityType Entity { get; }
    public IConceptualProperty ConceptualProperty { get; }
    public string EntityName => Entity?.Name;
}

public sealed class EntityProperty : EntityPropertyBase {
    public EntityProperty(EntityType conceptualEntity, ConceptualProperty conceptualProperty) : base(conceptualEntity,
        conceptualProperty) {
    }


    /// <summary> the database type name </summary>
    public string DbTypeName => StorageProperty?.Type;

    /// <summary> the conceptual (code) type name </summary>
    public string TypeName => ConceptualProperty?.Type;

    private EnumType enumType;
    public EnumType EnumType { get => enumType; set => SetProperty(ref enumType, value, OnEnumTypeChanged); }
    private void OnEnumTypeChanged() { }

    public bool Nullable => StorageProperty?.Nullable?.StartsWith("t", StringComparison.OrdinalIgnoreCase) == true;
    public bool IsIdentity => StorageProperty?.StoreGeneratedPattern == "Identity";

    public StorageProperty StorageProperty { get; set; }
    public ScalarPropertyMapping Mapping { get; set; }
    public string DbColumnName => Mapping?.ColumnName;

    /// <summary> The cleansed storage column name (spaces converted to underscore) </summary>
    public string DbColumnNameCleansed => DbColumnName.CleanseSymbolName();

    public new ConceptualProperty ConceptualProperty => (ConceptualProperty)base.ConceptualProperty;
    public bool IsConceptualKey { get; set; }
    public bool IsStorageKey { get; set; }

    public override string ToString() { return $"Prop: {Name}"; }
}

public sealed class NavigationProperty : EntityPropertyBase {
    public NavigationProperty(EntityType conceptualEntity, ConceptualNavigationProperty conceptualProperty)
        : base(conceptualEntity, conceptualProperty) {
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
        if (o?.NavigationProperties.Contains(this) == true) o.NavigationProperties.Remove(this);
        if (association?.NavigationProperties.Contains(this) == false) association.NavigationProperties.Add(this);
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

    /// <summary> true if this is the dependent end of the association. </summary>
    public bool IsDependentEnd { get; set; }

    /// <summary> true if this is the principal end of the association. </summary>
    public bool IsPrincipalEnd => !IsDependentEnd;

    /// <summary> the underlying foreign key properties that establish this association on this side of the relationship (i.e. this entity) </summary>
    public EntityProperty[] FkProperties { get; set; }

    /// <summary> The navigation property at the other end of this relationship (on the other entity). </summary>
    public NavigationProperty InverseNavigation { get; set; }

    public override string ToString() { return $"NProp: {Name}"; }
}

/// <summary>
/// Association that has no conceptual entity foreign key such as many-to-many associations where the junction table
/// has no conceptual representation, and therefore, no entity level dependent fields.
/// Further, the entities on both ends can both be considered principals.  There is no principal/dependent relationship here.
/// </summary>
public sealed class DesignAssociation : AssociationBase {
    public DesignAssociation(ConceptualAssociation conceptualAssociation, IList<EndRole> roles,
        NavigationProperty fromNavigation, NavigationProperty toNavigation) : base(
        conceptualAssociation, roles, fromNavigation, toNavigation) {
    }


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
            navigation.IsDependentEnd = isDependent;
            navigation.FkProperties = isDependent ? constraint.DependentProperties : constraint.PrincipalProperties;
        }
    }

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
            Navigations.Add(navigation);
            Debug.Assert(navigation.Association == null);
            navigation.Association = this;
            navigation.ToRole = roles.Single(o => o.Role == navigation.ToRoleName);
            navigation.FromRole = roles.Single(o => o.Role == navigation.FromRoleName);
            navigation.InverseNavigation = navigations.Single(o => o != navigation);
            Debug.Assert(navigation.InverseNavigation.Entity == navigation.ToRole.Entity);
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
    public IList<NavigationProperty> Navigations { get; set; } = new List<NavigationProperty>();

    /// <summary> Gets this is the relationship name. </summary>
    public string Name => ConceptualAssociation.Name;

    public ConceptualAssociation ConceptualAssociation { get; }
    public IList<EndRole> EndRoles { get; }
    public IList<NavigationProperty> NavigationProperties { get; } = new List<NavigationProperty>();
    public Schema Schema { get; }

    public bool IsSelfReference => EndRoles?.Count > 0 && EndRoles[0].Entity != null &&
                                   EndRoles.All(o => o.Entity == EndRoles[0].Entity);

    public override string ToString() { return $"Association: {Name}"; }
}

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

public enum Multiplicity {
    Unknown = 0,
    ZeroOne,
    One,
    Many
}

public sealed class EndRole : NotifyPropertyChanged {
    public EndRole(ConceptualAssociation conceptualAssociation, ConceptualEnd conceptualEnd, EntityType entityType) {
        ConceptualAssociation = conceptualAssociation;
        ConceptualEnd = conceptualEnd ?? throw new ArgumentNullException(nameof(conceptualEnd));
        Entity = entityType ?? throw new ArgumentNullException(nameof(entityType));

        Multiplicity = conceptualEnd.Multiplicity switch {
            "0..1" => Multiplicity.ZeroOne,
            "1" => Multiplicity.One,
            "*" => Multiplicity.Many,
            _ => throw new InvalidDataException("Multiplicity unexpected: " + conceptualEnd.Multiplicity)
        };
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
        ConceptualReferentialConstraint conceptualReferentialConstraint,
        EntityProperty[] principalProps,
        EntityProperty[] depProps) {
        ConceptualReferentialConstraint = conceptualReferentialConstraint;
#if DEBUG
        if (principalProps.IsNullOrEmpty()) throw new ArgumentNullException(nameof(principalProps));
        if (depProps.IsNullOrEmpty()) throw new ArgumentNullException(nameof(depProps));
#endif

        PrincipalProperties = principalProps?.ToArray() ?? Array.Empty<EntityProperty>();
        DependentProperties = depProps?.ToArray() ?? Array.Empty<EntityProperty>();
        PrincipalEntity = PrincipalProperties.FirstOrDefault()?.Entity;
        DependentEntity = DependentProperties.FirstOrDefault()?.Entity;
    }

    public ConceptualReferentialConstraint ConceptualReferentialConstraint { get; }

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