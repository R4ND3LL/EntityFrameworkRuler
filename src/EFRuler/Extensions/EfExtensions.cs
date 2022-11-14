using EdmxRuler.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkRuler.Extensions;

/// <summary>
/// Responsible for making the Entity Framework Metadata more
/// accessible for code generation.
/// </summary>
internal static class EfExtensions {
    /// <summary>
    /// This method returns the underlying CLR type of the o-space type corresponding to the supplied <paramref name="typeUsage"/>
    /// Note that for an enum type this means that the type backing the enum will be returned, not the enum type itself.
    /// </summary>
    public static Type ClrType(this IAnnotatable typeUsage) {
        if (typeUsage is IPropertyBase p) return UnderlyingClrType(p);
        return typeof(object);
    }

    /// <summary> Returns the name of the supplied object. </summary>
    public static string GetGlobalItemName(this IAnnotatable item) {
        switch (item) {
            case IEntityType e:
                var i = e.Name.LastIndexOf('.');
                if (i > 0) return e.Name.Substring(i + 1); // remove namespace
                return e.Name;
            case IDbFunction f:
                return f.Name;
            case ISequence s:
                return s.Name;
            case IProperty p:
                return p.Name;
            default: {
                var prop = item?.GetType().GetProperty("Name");
                return (string)prop?.GetValue(item);
            }
        }
    }

    /// <summary> Returns the namespace of the entity </summary>
    public static string GetNamespace(this IEntityType e) {
        var i = e.Name.LastIndexOf('.');
        if (i > 0) return e.Name.Substring(0, i - 1); // remove namespace
        return string.Empty;
    }

    /// <summary> Returns the table name of the supplied entity. </summary>
    public static string GetEntityTableName(this ITypeBase tb) {
        if (tb is not IEntityType entityType) return null;
        var tableName = entityType.GetTableName();
        return tableName;
    }

    /// <summary> Returns the table ident of the supplied entity. </summary>
    public static StoreObjectIdentifier GetStoreObjectIdentifier(this ITypeBase tb) {
        if (tb is not IEntityType entityType) return default;
        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema();
        var tableIdentifier = tableName != null ? StoreObjectIdentifier.Table(tableName, schema) : default;
        return tableIdentifier;
    }

    /// <summary> Returns the table ident of the supplied property. </summary>
    public static StoreObjectIdentifier GetStoreObjectIdentifier(this IProperty propertyType) {
        return propertyType.DeclaringType.GetStoreObjectIdentifier();
    }

    /// <summary> Returns the table column name for the supplied property </summary>
    public static string GetColumnName(this IProperty propertyType) {
        var tb = propertyType.DeclaringType;
        var tableIdentifier = tb.GetStoreObjectIdentifier();
        if (tableIdentifier == default || string.IsNullOrEmpty(tableIdentifier.Name)) return null;
        var tableColumnName = propertyType.GetColumnName(tableIdentifier);
        return tableColumnName;
    }

    /// <summary> Returns true if the property value generation strategy is set to "Identity" </summary>
    public static bool IsStoreGeneratedIdentity(this IProperty propertyType) {
        var annotations = propertyType.GetAnnotations();
        const string key2 = "ValueGenerationStrategy";
        var isIdentity = annotations.Where(o => o.Name.EndsWith(key2))
            .Any(o => o.Value?.ToString()?.ToLower().Contains("identity") == true);
        return isIdentity;
    }

    /// <summary> Returns true if the property value generation strategy is set to "Computed" </summary>
    public static bool IsStoreGeneratedComputed(this IProperty propertyType) {
        var annotations = propertyType.GetAnnotations();
        const string key2 = "ValueGenerationStrategy";
        var isIdentity = annotations.Where(o => o.Name.EndsWith(key2))
            .Any(o => o.Value?.ToString()?.ToLower().Contains("computed") == true);
        return isIdentity;
    }

    /// <summary>
    /// This method returns the underlying CLR type given the c-space type.
    /// Note that for an enum type this means that the type backing the enum will be returned, not the enum type itself.
    /// </summary>
    public static Type UnderlyingClrType(this IPropertyBase edmType) {
        if (edmType?.ClrType != null) {
            if (edmType.ClrType.IsEnum) edmType.ClrType.GetEnumUnderlyingType();
            return edmType.ClrType;
        }

        //var primitiveType = edmType as IProperty;
        //if (primitiveType != null) return primitiveType.ClrType;

        //var enumType = edmType as EnumType;
        //if (enumType != null) return enumType.UnderlyingType.ClrEquivalentType;

        return typeof(object);
    }

    /// <summary>
    /// True if the IPropertyBase is a key of its DeclaringType, False otherwise.
    /// </summary>
    public static bool IsKey(this IPropertyBase property) {
        //if (property != null && property.DeclaringType.BuiltInTypeKind == BuiltInTypeKind.IEntityType) return ((IEntityType)property.DeclaringType).KeyMembers.Contains(property);
        if (property is IProperty p) return p.IsKey();
        return false;
    }

    /// <summary>
    /// True if the IPropertyBase TypeUsage is Nullable, False otherwise.
    /// </summary>
    public static bool IsNullable(this IPropertyBase property) {
        if (property is IProperty p) return p.IsNullable;
        if (property is INavigation n) return !n.IsCollection && n.ForeignKey.Properties.All(o => o.IsNullable);
        return false;
    }

    // /// <summary>
    // /// True if the TypeUsage is Nullable, False otherwise.
    // /// </summary>
    // public static bool IsNullable(TypeUsage typeUsage) {
    //     Facet nullableFacet = null;
    //     if (typeUsage != null &&
    //         typeUsage.Facets.TryGetValue("Nullable", true, out nullableFacet))
    //         return (bool)nullableFacet.Value;
    //
    //     return false;
    // }
    //
    // /// <summary>
    // /// If the passed in TypeUsage represents a collection this method returns final element
    // /// type of the collection, otherwise it returns the value passed in.
    // /// </summary>
    // public static TypeUsage GetElementType(TypeUsage typeUsage) {
    //     if (typeUsage == null) return null;
    //
    //     if (typeUsage.EntityType is CollectionType) return GetElementType(((CollectionType)typeUsage.EntityType).TypeUsage);
    //
    //     return typeUsage;
    // }

    /// <summary>
    /// Returns the INavigation that is the other end of the same association set if it is
    /// available, otherwise it returns null.
    /// </summary>
    public static INavigation Inverse(this INavigation navProperty) {
        return navProperty?.Inverse;
        // return toEntity.NavigationProperties
        //     .SingleOrDefault(n => Object.ReferenceEquals(n.RelationshipType, navProperty.RelationshipType) && !Object.ReferenceEquals(n, navProperty));
    }

    /// <summary>
    /// Given a property on the dependent end of a referential constraint, returns the corresponding property on the principal end.
    /// Requires: The association has a referential constraint, and the specified dependentProperty is one of the properties on the dependent end.
    /// </summary>
    public static IPropertyBase GetCorrespondingPrincipalProperty(this INavigation navProperty,
        IProperty dependentProperty) {
        if (navProperty == null) throw new ArgumentNullException(nameof(navProperty));

        if (dependentProperty == null) throw new ArgumentNullException(nameof(dependentProperty));

        var fromProperties = GetPrincipalProperties(navProperty);
        var toProperties = GetDependentProperties(navProperty);
        if (fromProperties.Contains(dependentProperty)) return dependentProperty;
        return fromProperties[toProperties.ToList().IndexOf(dependentProperty)];
    }

    /// <summary>
    /// Given a property on the principal end of a referential constraint, returns the corresponding property on the dependent end.
    /// Requires: The association has a referential constraint, and the specified principalProperty is one of the properties on the principal end.
    /// </summary>
    public static IPropertyBase GetCorrespondingDependentProperty(this INavigation navProperty,
        IProperty principalProperty) {
        if (navProperty == null) throw new ArgumentNullException(nameof(navProperty));

        if (principalProperty == null) throw new ArgumentNullException(nameof(principalProperty));

        var fromProperties = GetPrincipalProperties(navProperty);
        var toProperties = GetDependentProperties(navProperty);
        if (toProperties.Contains(principalProperty)) return principalProperty;
        return toProperties[fromProperties.ToList().IndexOf(principalProperty)];
    }

    /// <summary>
    /// Gets the collection of properties that are on the principal end of a referential constraint for the specified navigation property.
    /// Requires: The association has a referential constraint.
    /// </summary>
    public static IReadOnlyList<IProperty> GetPrincipalProperties(this INavigation navProperty) {
        if (navProperty == null) throw new ArgumentNullException(nameof(navProperty));

        //return ((AssociationType)navProperty.RelationshipType).ReferentialConstraints[0].FromProperties;
        return navProperty.ForeignKey.PrincipalKey.Properties;
    }

    /// <summary>
    /// Gets the collection of properties that are on the dependent end of a referential constraint for the specified navigation property.
    /// Requires: The association has a referential constraint.
    /// </summary>
    public static IReadOnlyList<IProperty> GetDependentProperties(this INavigation navProperty) {
        if (navProperty == null) throw new ArgumentNullException(nameof(navProperty));

        return navProperty.ForeignKey.Properties;
        //return ((AssociationType)navProperty.RelationshipType).ReferentialConstraints[0].ToProperties;
    }

    /// <summary>
    /// True if this entity type requires the HandleCascadeDelete method defined and the method has
    /// not been defined on any base type
    /// </summary>
    public static bool NeedsHandleCascadeDeleteMethod(this IModel itemCollection, IEntityType entity) {
        var needsMethod = ContainsCascadeDeleteAssociation(entity);
        // Check to make sure no base types have already declared this method
        var baseType = entity.BaseType as IEntityType;
        while (needsMethod && baseType != null) {
            needsMethod = !ContainsCascadeDeleteAssociation(baseType);
            baseType = baseType.BaseType as IEntityType;
        }

        return needsMethod;
    }

    /// <summary>
    /// True if this entity type participates in any relationships where the other end has an OnDelete
    /// cascade delete defined, or if it is the dependent in any identifying relationships
    /// </summary>
    private static bool ContainsCascadeDeleteAssociation(IEntityType entity) {
        return entity.GetNavigations().Any(IsCascadeDeletePrincipal);
    }

    /// <summary>
    /// True if the source end of the specified navigation property is the principal in an identifying relationship.
    /// or if the source end has cascade delete defined.
    /// </summary>
    public static bool IsCascadeDeletePrincipal(this INavigation associationEnd) {
        if (associationEnd == null) throw new ArgumentNullException(nameof(associationEnd));
        var db = GetDeleteBehavior(associationEnd);
        return db == DeleteBehavior.Cascade || IsPrincipalEndOfIdentifyingRelationship(associationEnd);
    }

    public static DeleteBehavior GetDeleteBehavior(this IPropertyBase prop) {
        return GetDeleteBehavior(prop as INavigation);
    }

    public static DeleteBehavior GetDeleteBehavior(this INavigation navProperty) {
        return GetDeleteBehavior(navProperty?.ForeignKey);
    }

    public static DeleteBehavior GetDeleteBehavior(this IReadOnlyForeignKey foreignKey) {
        return foreignKey?.DeleteBehavior ?? DeleteBehavior.NoAction;
    }

    public static bool IsManyToMany(this IReadOnlyForeignKey foreignKey) {
        return foreignKey.GetNavigation(true)?.IsCollection == true &&
               foreignKey.GetNavigation(false)?.IsCollection == true;
    }

    public static IReadOnlyNavigation GetDependentNavigation(this IReadOnlyForeignKey foreignKey) =>
        foreignKey.GetNavigation(true);

    public static IReadOnlyNavigation GetPrincipalNavigation(this IReadOnlyForeignKey foreignKey) =>
        foreignKey.GetNavigation(false);

    /// <summary>
    /// True if the specified association end is the principal end in an identifying relationship.
    /// In order to be an identifying relationship, the association must have a referential constraint where all of the dependent properties are part of the dependent type's primary key.
    /// </summary>
    public static bool IsPrincipalEndOfIdentifyingRelationship(this INavigation associationEnd) {
        if (associationEnd == null) throw new ArgumentNullException(nameof(associationEnd));

        if (associationEnd.IsOnDependent) return false;
        var principalEntity = associationEnd.DeclaringType;
        if (principalEntity is not IEntityType et) return false;
        var key = et.FindPrimaryKey();
        var keyMembers = key?.Properties ?? new List<IProperty>();
        return associationEnd.ForeignKey.PrincipalKey.Properties.All(o => keyMembers.Contains(o));
    }

    /// <summary>
    /// True if the specified association type is an identifying relationship.
    /// In order to be an identifying relationship, the association must have a referential constraint where all of the dependent properties are part of the dependent type's primary key.
    /// </summary>
    public static bool IsIdentifyingRelationship(this INavigation association) {
        if (association == null) throw new ArgumentNullException(nameof(association));
        return IsPrincipalEndOfIdentifyingRelationship(association) ||
               IsPrincipalEndOfIdentifyingRelationship(association.Inverse);
    }

    /// <summary> True if this is a one to one relationship </summary>
    public static bool IsOneToOne(this INavigation navProperty) {
        if (navProperty == null) return false;
        if (navProperty.IsCollection || navProperty.Inverse?.IsCollection != false) return false;
        if (!navProperty.ForeignKey.IsUnique) return false;
        // extra check:
        var principals = GetPrincipalProperties(navProperty);
        var principalEntity = principals.FirstOrDefault()?.DeclaringEntityType;
        if (principalEntity is not IEntityType et) return false;
        var key = et.FindPrimaryKey();
        var keyMembers = key?.Properties ?? new List<IProperty>();
        if (!navProperty.ForeignKey.PrincipalKey.Properties.All(o => keyMembers.Contains(o))) return false;

        var dependents = GetDependentProperties(navProperty);
        var dependentEntity = dependents.FirstOrDefault()?.DeclaringEntityType;
        if (dependentEntity is not IEntityType et2) return false;
        key = et2.FindPrimaryKey();
        keyMembers = key?.Properties ?? new List<IProperty>();
        return navProperty.ForeignKey.Properties.All(o => keyMembers.Contains(o));
    }

    /// <summary>
    /// requires: firstType is not null
    /// effects: if secondType is among the base types of the firstType, return true,
    /// otherwise returns false.
    /// when firstType is same as the secondType, return false.
    /// </summary>
    public static bool IsSubtypeOf(this IEntityType firstType, IEntityType secondType) {
        if (secondType == null) return false;

        // walk up firstType hierarchy list
        for (var t = firstType.BaseType; t != null; t = t.BaseType)
            if (t == secondType)
                return true;
        return false;
    }

    public static void ArgumentNotNull<T>(this T arg, string name) where T : class {
        if (arg == null) {
            throw new ArgumentNullException(name);
        }
    }

    public static bool IsView(this IEntityType entity) => GetViewName(entity).HasNonWhiteSpace();

    public static string GetViewName(this IEntityType entity) {
        var viewName = entity?.FindAnnotation("Relational:ViewName");
        return viewName?.Value?.ToString();
    }

    /// <summary> get DbType enum for a property </summary>
    public static System.Data.DbType? GetDbType(IProperty property) {
        var mapping = property.GetTypeMapping();
        if (mapping is RelationalTypeMapping rMapping) {
            return rMapping.DbType;
        }

        return default;
    }

    public static TypeScaffoldingInfo WithType(this TypeScaffoldingInfo typeScaffoldingInfo, Type clrType) {
        if (clrType == null) throw new ArgumentNullException(nameof(clrType));
        return new(
            clrType,
            typeScaffoldingInfo?.IsInferred ?? false,
            typeScaffoldingInfo?.ScaffoldUnicode,
            typeScaffoldingInfo?.ScaffoldMaxLength,
            typeScaffoldingInfo?.ScaffoldFixedLength,
            typeScaffoldingInfo?.ScaffoldPrecision,
            typeScaffoldingInfo?.ScaffoldScale);
    }
}