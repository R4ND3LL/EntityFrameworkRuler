using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using EdmxRuler.Common;
using EdmxRuler.Extensions;
using EdmxRuler.Generator.EdmxModel;

// ReSharper disable MemberCanBeInternal
// ReSharper disable once ClassCanBeSealed.Global
// ReSharper disable once MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global

namespace EdmxRuler.Generator.Services;

public class EdmxRulerNamingService : IEdmxRulerNamingService {
    public EdmxRulerNamingService(GeneratorOptions options = null, IEdmxRulerPluralizer pluralizer = null) {
        this.options = options ?? new GeneratorOptions();
        this.pluralizer = pluralizer ?? new HumanizerPluralizer();
        cSharpUtilities = new();
        tableNamer = new CSharpUniqueNamer<EntityType>(
            this.options.UseDatabaseNames
                ? (t => t.Name)
                : GenerateCandidateIdentifier,
            cSharpUtilities,
            this.options.NoPluralize
                ? null
                : this.pluralizer.Singularize);
        columnNamers = new();
    }

    private readonly GeneratorOptions options;
    private readonly IEdmxRulerPluralizer pluralizer;
    private readonly CSharpUtilities cSharpUtilities;
    private readonly CSharpUniqueNamer<EntityType> tableNamer;
    private readonly Dictionary<EntityType, CSharpUniqueNamer<EntityProperty>> columnNamers;

    public bool RelyOnEfMethodOnly { get; } = true;

    public IEnumerable<string> FindCandidateNavigationNames(NavigationProperty navigation) {
        var ass = navigation.Association;

        if (navigation.Entity.Name == "WorkPool" && navigation.ToRole.Entity.Name == "Employee") Debugger.Break();

        var isMany = navigation.Multiplicity == Multiplicity.Many;
        switch (ass) {
            case FkAssociation fkAssociation: {
                var foreignKey = fkAssociation.ReferentialConstraint;
                if (foreignKey == null) yield break;

                var deps = foreignKey.DependentProperties;
                var dep = deps?.FirstOrDefault();
                if (dep == null) yield break;

                var inverseEntity = navigation.InverseNavigation.Entity;

                // EF methodology for determining names:
                var dependentEndExistingIdentifiers =
                    foreignKey.DependentEntity.GetExistingIdentifiers(this, navigation); //foreignKey.DeclaringEntityType);
                var dependentEndNavigationPropertyCandidateName =
                    GetDependentEndCandidateNavigationPropertyName(foreignKey);
                var dependentEndNavigationPropertyName =
                    cSharpUtilities.GenerateCSharpIdentifier(
                        dependentEndNavigationPropertyCandidateName,
                        dependentEndExistingIdentifiers,
                        singularizePluralizer: null,
                        uniquifier: NavigationUniquifier);
                if (navigation.IsDependentEnd) {
                    // typically entity references (not collections)
                    Debug.Assert(navigation.Multiplicity != Multiplicity.Many);
                    yield return dependentEndNavigationPropertyName;
                }

                if (navigation.IsPrincipalEnd) {
                    // typically collections.  but may be 1-1 reference
                    var principalEndExistingIdentifiers = foreignKey.PrincipalEntity.GetExistingIdentifiers(this, navigation);
                    var principalEndNavigationPropertyCandidateName = foreignKey.IsSelfReferencing()
                        ? string.Format(
                            CultureInfo.CurrentCulture,
                            SelfReferencingPrincipalEndNavigationNamePattern,
                            dependentEndNavigationPropertyName)
                        : GetPrincipalEndCandidateNavigationPropertyName(foreignKey,
                            dependentEndNavigationPropertyName);

                    if (!foreignKey.IsUnique && !foreignKey.IsSelfReferencing())
                        principalEndNavigationPropertyCandidateName = options.NoPluralize
                            ? principalEndNavigationPropertyCandidateName
                            : pluralizer.Pluralize(principalEndNavigationPropertyCandidateName);

                    var principalEndNavigationPropertyName =
                        cSharpUtilities.GenerateCSharpIdentifier(
                            principalEndNavigationPropertyCandidateName,
                            principalEndExistingIdentifiers,
                            singularizePluralizer: null,
                            uniquifier: NavigationUniquifier);

                    yield return principalEndNavigationPropertyName;
                }

                if (RelyOnEfMethodOnly) yield break;

                // last ditch efforts. They usually catch most cases
                var prefix = navigation.IsDependentEnd ? string.Empty : inverseEntity.StorageNameIdentifier;

                if (isMany) {
                    yield return $"{prefix}{dep.DbColumnNameIdentifier}Navigations";
                    yield return pluralizer.Pluralize(navigation.ToRole.Entity.StorageNameIdentifier);
                } else {
                    yield return $"{prefix}{dep.DbColumnNameIdentifier}Navigation";
                    yield return navigation.ToRole.Entity.StorageNameIdentifier;
                }

                break;
            }
            // likely a many-to-many relation where the junction is gone
            case DesignAssociation designAssociation when isMany:
                yield return pluralizer.Pluralize(navigation.ToRole.Entity.StorageNameIdentifier);
                break;
            case DesignAssociation designAssociation:
                yield return navigation.ToRole.Entity.StorageNameIdentifier;
                break;
        }
    }

    public string GetExpectedEntityTypeName(EntityType entity) {
        var v = entity.GetExpectedEfCoreName(this);
        if (v != null) return v;
        return entity.SetExpectedEfCoreName(this, tableNamer.GetName(entity));
    }

    public string GetExpectedPropertyName(EntityProperty column, string expectedEntityTypeName = null) {
        var v = column.GetExpectedEfCoreName(this);
        if (v != null) return v;

        var table = column.Entity;
        var usedNames = new List<string>();
        if (column.Entity != null) usedNames.Add(expectedEntityTypeName.CoalesceWhiteSpace(() => GetExpectedEntityTypeName(table)));

        return column.SetExpectedEfCoreName(this, columnNamers.GetOrAddNew(table, Factory).GetName(column));

        CSharpUniqueNamer<EntityProperty> Factory(EntityType _) {
            if (options.UseDatabaseNames)
                return new(
                    c => c.DbColumnName,
                    usedNames,
                    cSharpUtilities,
                    singularizePluralizer: null);
            return new(
                GenerateCandidateIdentifier,
                usedNames,
                cSharpUtilities,
                singularizePluralizer: null);
        }
    }


    public string GenerateCandidateIdentifier(EntityType originalTable) => originalTable.StorageNameIdentifier;
    public string GenerateCandidateIdentifier(EntityProperty originalColumn) => originalColumn.DbColumnNameIdentifier;

    /// <summary> Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal.CandidateNamingService </summary>
    private string GetDependentEndCandidateNavigationPropertyName(ReferentialConstraint foreignKey) {
        //     Gets the foreign key properties in the dependent entity.
        var properties = foreignKey.DependentProperties;
        var candidateName = FindCandidateNavigationName(properties);

        return !string.IsNullOrEmpty(candidateName)
                ? candidateName
                : GetExpectedEntityTypeName(foreignKey.PrincipalEntity)
            ; // was ShortName(), which pulls ClrType ShortDisplayName and generally always just returns the flat type name
    }

    /// <summary> Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal.CandidateNamingService </summary>
    private string GetPrincipalEndCandidateNavigationPropertyName(ReferentialConstraint foreignKey,
        string dependentEndNavigationPropertyName) {
        var allForeignKeysBetweenDependentAndPrincipal =
            foreignKey.PrincipalEntity.GetReferencingForeignKeys()
                .Where(fk => foreignKey.DependentEntity == fk.DependentEntity);

        return allForeignKeysBetweenDependentAndPrincipal?.Count() > 1
            ? GetExpectedEntityTypeName(foreignKey.DependentEntity)
              + dependentEndNavigationPropertyName
            : GetExpectedEntityTypeName(foreignKey.DependentEntity);
    }

    /// <summary> Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal.CandidateNamingService </summary>
    private string FindCandidateNavigationName(IList<EntityProperty> properties) {
        var count = properties.Count;
        if (count == 0) return string.Empty;

        var firstProperty = properties.First();
        var firstPropertyName = GetExpectedPropertyName(firstProperty);
        return StripId(
            count == 1
                ? firstPropertyName
                : FindCommonPrefix(firstPropertyName, properties.Select(p => GetExpectedPropertyName(p))));
    }

    /// <summary> Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal.CandidateNamingService </summary>
    private static string FindCommonPrefix(string firstName, IEnumerable<string> propertyNames) {
        var prefixLength = 0;
        foreach (var c in firstName) {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var s in propertyNames)
                if (s.Length <= prefixLength
                    || s[prefixLength] != c)
                    return firstName[..prefixLength];

            prefixLength++;
        }

        return firstName[..prefixLength];
    }

    /// <summary> Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal.CandidateNamingService </summary>
    private static string StripId(string commonPrefix) {
        if (commonPrefix.Length < 3
            || !commonPrefix.EndsWith("id", StringComparison.OrdinalIgnoreCase))
            return commonPrefix;

        var ignoredCharacterCount = 2;
        if (commonPrefix.Length > 4
            && commonPrefix.EndsWith("guid", StringComparison.OrdinalIgnoreCase))
            ignoredCharacterCount = 4;

        int i;
        for (i = commonPrefix.Length - ignoredCharacterCount - 1; i >= 0; i--)
            if (char.IsLetterOrDigit(commonPrefix[i]))
                break;

        return i != 0
            ? commonPrefix[..(i + 1)]
            : commonPrefix;
    }

    internal const string NavigationNameUniquifyingPattern = "{0}Navigation";

    internal const string SelfReferencingPrincipalEndNavigationNamePattern = "Inverse{0}";

    private static string NavigationUniquifier(string proposedIdentifier, ICollection<string> existingIdentifiers) {
        if (existingIdentifiers?.Contains(proposedIdentifier) != true) return proposedIdentifier;

        var finalIdentifier =
            string.Format(CultureInfo.CurrentCulture, NavigationNameUniquifyingPattern, proposedIdentifier);
        var suffix = 1;
        while (existingIdentifiers.Contains(finalIdentifier)) {
            finalIdentifier = proposedIdentifier + suffix;
            suffix++;
        }

        return finalIdentifier;
    }
}