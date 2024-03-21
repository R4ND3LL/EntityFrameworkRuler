using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntityFrameworkRuler.Design.Extensions;

/// <summary>
/// Responsible for making the Entity Framework Metadata more
/// accessible for code generation.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
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

    /// <summary> Returns the table or view schema name of the supplied entity. </summary>
    public static string GetTableOrViewSchema(this IMutableEntityType tb) {
        if (tb is not IEntityType entityType) return null;
        var schemaName = entityType.GetSchema();
        schemaName ??= entityType.GetViewSchema();
        return schemaName;
    }

    /// <summary> Returns the table or view name of the supplied entity. </summary>
    public static string GetTableOrViewName(this IMutableEntityType tb) {
        if (tb is not IEntityType entityType) return null;
        var tableName = entityType.GetTableName();
        tableName ??= entityType.GetViewName();
        return tableName;
    }

    /// <summary> Returns the table ident of the supplied entity. </summary>
    public static StoreObjectIdentifier GetStoreObjectIdentifier(this ITypeBase tb) {
        if (tb is not IEntityType entityType) return default;
        var name = entityType.GetTableName();
        if (name != null) return StoreObjectIdentifier.Table(name, entityType.GetSchema());

        name = RelationalEntityTypeExtensions.GetViewName(entityType);
        return name == null ? default : StoreObjectIdentifier.View(name, entityType.GetViewSchema());
    }

    /// <summary> Returns the table ident of the supplied entity. </summary>
    public static StoreObjectIdentifier GetStoreObjectIdentifier(this IMutableTypeBase tb) {
        if (tb is not IEntityType entityType) return default;
        var name = entityType.GetTableName();
        if (name != null) return StoreObjectIdentifier.Table(name, entityType.GetSchema());

        name = RelationalEntityTypeExtensions.GetViewName(entityType);
        return name == null ? default : StoreObjectIdentifier.View(name, entityType.GetViewSchema());
    }

    /// <summary> Returns the table ident of the supplied property. </summary>
    public static StoreObjectIdentifier GetStoreObjectIdentifier(this IProperty propertyType) {
        return propertyType.DeclaringType.GetStoreObjectIdentifier();
    }

    /// <summary> Returns the table column name for the supplied property </summary>
    public static string GetColumnNameNoDefault(this IMutableProperty o) {
        return o.FindAnnotation(RelationalAnnotationNames.ColumnName)?.Value as string; //?? o.GetDefaultColumnName();
    }

    /// <summary> Returns the table column name for the supplied property </summary>
    public static string GetColumnNameUsingStoreObject(this IProperty propertyType) {
        var tb = propertyType.DeclaringType;
        var tableIdentifier = tb.GetStoreObjectIdentifier();
        if (tableIdentifier == default || string.IsNullOrEmpty(tableIdentifier.Name)) return null;
        var tableColumnName = propertyType.GetColumnName(tableIdentifier);
        if (tableColumnName.IsNullOrEmpty()) {
            tableColumnName = propertyType.FindAnnotation("Relational:Name")?.Value as string;
        }

        return tableColumnName;
    }

    /// <summary> Returns the table column name for the supplied property </summary>
    public static string GetColumnNameUsingStoreObject(this IMutableProperty propertyType) {
        var tb = propertyType.DeclaringType;
        var tableIdentifier = tb.GetStoreObjectIdentifier();
        if (tableIdentifier == default || string.IsNullOrEmpty(tableIdentifier.Name)) return null;
        var tableColumnName = propertyType.GetColumnName(tableIdentifier);
        if (tableColumnName.IsNullOrEmpty()) {
            tableColumnName = propertyType.FindAnnotation("Relational:Name")?.Value as string;
        }

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

    public static Multiplicity GetMultiplicity(this INavigation property) {
        if (property == null) return Multiplicity.Unknown;
        if (property.IsCollection) return Multiplicity.Many;
        var props = property.ForeignKey?.Properties;
        if (props == null || props.Count == 0) return Multiplicity.Unknown;
        if (props.All(o => o.IsNullable)) return Multiplicity.ZeroOne;
        return Multiplicity.One;
    }

    public static Multiplicity GetMultiplicity(this IMutableNavigation property) {
        if (property == null) return Multiplicity.Unknown;
        if (property.IsCollection) return Multiplicity.Many;
        var props = property.ForeignKey?.Properties;
        if (props == null || props.Count == 0) return Multiplicity.Unknown;
        if (props.All(o => o.IsNullable)) return Multiplicity.ZeroOne;
        return Multiplicity.One;
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

    /// <summary>
    ///     Check whether an ScaffoldedTable could be considered a simple many-to-many join table, often suppressed dy EF.
    /// </summary>
    /// <param name="table">The ScaffoldedTable to check.</param>
    /// <returns><see langword="true" /> if the ScaffoldedTable could be considered a join table.</returns>
    public static bool IsSimpleManyToManyJoinEntityType(this DatabaseTable table) {
        //if (entityType.GetNavigations().Any() || entityType.GetSkipNavigations().Any()) return false;
        var primaryKey = table.PrimaryKey;
        var properties = table.Columns;
        var foreignKeys = table.ForeignKeys;
        //var indexes = table.Indexes;
        if (primaryKey != null
            && primaryKey.Columns.Count > 1
            && foreignKeys.Count == 2
            && primaryKey.Columns.Count == properties.Count
            && foreignKeys[0].Columns.Count + foreignKeys[1].Columns.Count == properties.Count
            && !foreignKeys[0].Columns.Intersect(foreignKeys[1].Columns).Any()
            && foreignKeys[0].Columns.All(o => !o.IsNullable)
            && foreignKeys[1].Columns.All(o => !o.IsNullable)
            // && !foreignKeys[0].IsUnique
            // && !foreignKeys[1].IsUnique
           ) {
            return true;
        }

        return false;
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

    /// <summary> Get schema.tableName </summary>
    public static string GetFullName(this DatabaseTable table) =>
        table?.Schema.IsNullOrEmpty() == false ? $"{table.Schema}.{table.Name}" : table?.Name ?? "";

    public static string GetViewName(this IEntityType entity) {
        var viewName = entity?.FindAnnotation("Relational:ViewName");
        return viewName?.Value?.ToString();
    }

    /// <summary> EF GetConstraintName() always returns null for views.  This method will pull the name annotation anyway. </summary>
    public static string GetConstraintNameForTableOrView(this IReadOnlyForeignKey foreignKey) {
        var fkName = foreignKey.GetConstraintName() ?? foreignKey.FindAnnotation(RelationalAnnotationNames.Name)?.Value as string;
        return fkName;
    }

    /// <summary> get DbType enum for a property </summary>
    public static DbType? GetDbType(IProperty property) {
        var mapping = property.GetTypeMapping();
        if (mapping is RelationalTypeMapping rMapping) {
            return rMapping.DbType;
        }

        return default;
    }

    /// <summary> Regenerate the TypeScaffoldingInfo based on our new CLR type. </summary>
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

    public static IList<DatabaseColumn> GetTableColumns(this DatabaseTable databaseTable, string[] props, StringComparer comparer) {
        if (databaseTable == null || props.IsNullOrEmpty()) return ArraySegment<DatabaseColumn>.Empty;
        //if (OmittedTables.Contains(scaffoldedTable.GetFullName())) return ArraySegment<DatabaseColumn>.Empty;
        var cols = props.Select(o => databaseTable.Columns.FirstOrDefault(c => comparer.Compare(c.Name, o) == 0)).ToArray();
        if (cols.Length == 0 || cols.Any(o => o == null)) return ArraySegment<DatabaseColumn>.Empty;
        return cols;
    }

    /// <summary>
    ///     Adds a <see cref="ServiceLifetime.Singleton" /> service implemented by the given concrete
    ///     type to the list of services that implement the given contract. The service is only added
    ///     if the collection contains no other registration for the same service and implementation type.
    /// </summary>
    /// <typeparam name="TService">The contract for the service.</typeparam>
    /// <typeparam name="TImplementation">The concrete type that implements the service.</typeparam>
    /// <returns>The service collection, such that further calls can be chained.</returns>
    public static IServiceCollection TryAddSingletonEnumerable
        <TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
            this IServiceCollection serviceCollection)
        where TService : class
        where TImplementation : class, TService {
        serviceCollection.TryAddEnumerable(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton));
        return serviceCollection;
    }

    /// <summary> Get the entity properties for the given DB columns </summary>
    public static IMutableProperty[] GetPropertiesFromDbColumns(this IMutableEntityType e, IEnumerable<DatabaseColumn> columns,
        IEqualityComparer<string> stringComparer) {
        return GetPropertiesFromDbColumns(e, columns?.Select(o => o.Name), stringComparer);
    }

    /// <summary> Get the entity properties for the given DB columns </summary>
    public static IMutableProperty[] GetPropertiesFromDbColumns(this IMutableEntityType e, IEnumerable<string> columns,
        IEqualityComparer<string> stringComparer) {
        if (columns == null) return Array.Empty<IMutableProperty>();
        var propsByDbName = e.GetProperties().Select(o => (dbName: o.GetColumnNameNoDefault(), prop: o))
            .Where(o => o.dbName.HasNonWhiteSpace())
            .DistinctBy(o => o.dbName)
            .ToDictionary(o => o.dbName, o => o.prop, stringComparer);
        var props = columns.Select(o => propsByDbName.TryGetValue(o)).ToArray();
        return props;
    }

    /// <summary> Get the entity property for the given DB column </summary>
    public static IMutableProperty GetPropertyFromDbColumn(this IMutableEntityType e, string column,
        StringComparison stringComparison) {
        if (column.IsNullOrWhiteSpace()) return null;
        var prop = e.GetProperties().Select(o => (dbName: o.GetColumnNameNoDefault(), prop: o))
            .Where(o => o.dbName.HasNonWhiteSpace())
            .FirstOrDefault(o => string.Equals(o.dbName, column, stringComparison));
        return prop.prop;
    }

    public static bool ColumnsAreEqual(this DatabaseForeignKey a, DatabaseForeignKey b, StringComparison stringComparison = StringComparison.Ordinal) {
        return a.Columns.ColumnsAreEqual(b.Columns, false, stringComparison) && a.PrincipalColumns.ColumnsAreEqual(b.PrincipalColumns, false, stringComparison);
    }

    /// <summary> return true if both column lists represent the same columns (schema details not checked) </summary>
    public static bool ColumnsAreEqual(this IList<DatabaseColumn> a, IList<DatabaseColumn> b, bool compareTable = true,
        StringComparison stringComparison = StringComparison.Ordinal) {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
            if (!ColumnsAreEqual(a[i], b[i], compareTable, stringComparison)) return false;

        return true;
    }

    /// <summary> return true if both columns are the same (schema details not checked) </summary>
    public static bool ColumnsAreEqual(this DatabaseColumn a, DatabaseColumn b, bool compareTable, StringComparison stringComparison) {
        if (compareTable) {
            if (!string.Equals(b.Table.Schema, a.Table.Schema, stringComparison)) return false;
            if (!string.Equals(b.Table.Name, a.Table.Name, stringComparison)) return false;
        }

        return string.Equals(b.Name, a.Name, stringComparison);
    }

    /// <summary> return true if the given column is a member of the foreign key </summary>
    public static bool HasColumn(this DatabaseForeignKey a, DatabaseColumn c, StringComparison stringComparison = StringComparison.Ordinal) {
        if (string.Equals(a.Table.Schema, c.Table.Schema, stringComparison) && string.Equals(a.Table.Name, c.Table.Name, stringComparison))
            foreach (var column in a.Columns) {
                if (string.Equals(column.Name, c.Name, stringComparison))
                    return true;
            }

        if (string.Equals(a.PrincipalTable.Schema, c.Table.Schema, stringComparison) && string.Equals(a.PrincipalTable.Name, c.Table.Name, stringComparison))
            foreach (var column in a.PrincipalColumns) {
                if (string.Equals(column.Name, c.Name, stringComparison))
                    return true;
            }

        return false;
    }

    public static bool TryConvertDefaultValue(this IReadOnlyProperty property, object value, out object newValue) {
        if (value == null || value == DBNull.Value) {
            newValue = value;
            return true;
        }

        var type = value.GetType();
        if (!property.ClrType.UnwrapNullableType().IsAssignableFrom(type)) {
            try {
                if (property.ClrType.IsEnum) {
                    if (type.IsNumericType()) {
                        // value is a number. ensure that it is the correct underlying type for the enum and then convert it.
                        var number = Convert.ChangeType(value, property.ClrType.GetEnumUnderlyingType(), CultureInfo.InvariantCulture);
                        newValue = Enum.ToObject(property.ClrType, number);
                        return true;
                    }
                    if (value is string s) {
                        newValue = Enum.Parse(property.ClrType, s, true);
                        return true;
                    }
                }
                newValue = Convert.ChangeType(value, property.ClrType, CultureInfo.InvariantCulture);
            } catch (Exception) {
                newValue = null;
                return false;
            }
        } else newValue = value;

        return true;
    }
}