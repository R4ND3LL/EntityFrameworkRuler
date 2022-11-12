using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using EdmxRuler.Common;
using EdmxRuler.Extensions;
using EdmxRuler.Rules.NavigationNaming;
using EdmxRuler.Rules.PrimitiveNaming;
using EntityFrameworkRuler.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Services;

/// <summary> Naming service override to be used by Ef scaffold process. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class EfCandidateNamingService : CandidateNamingService {
    private readonly PrimitiveNamingRules primitiveNamingRules;
    private readonly NavigationNamingRules navigationRules;

    /// <inheritdoc />
    public EfCandidateNamingService(IRuleProvider ruleProvider) {
#if DEBUG
        if (!Debugger.IsAttached) Debugger.Launch();
#endif
        primitiveNamingRules = ruleProvider?.GetPrimitiveNamingRules() ?? new PrimitiveNamingRules();
        navigationRules = ruleProvider?.GetNavigationNamingRules() ?? new NavigationNamingRules();
        //this.preserveCasingUsingRegex = preserveCasingUsingRegex;
    }

    /// <summary> Name that table </summary>
    public override string GenerateCandidateIdentifier(DatabaseTable originalTable) {
        if (originalTable == null) throw new ArgumentException("Argument is empty", nameof(originalTable));

        var candidateStringBuilder = new StringBuilder();

        var schema = GetSchemaReference(originalTable.Schema);

        if (schema == null) return base.GenerateCandidateIdentifier(originalTable);

        if (schema.UseSchemaName) candidateStringBuilder.Append(GenerateIdentifier(originalTable.Schema));

        var newTableName = string.Empty;

        if (schema.Tables != null && schema.Tables.Any(t => t.Name == originalTable.Name))
            newTableName = schema.Tables.SingleOrDefault(t => t.Name == originalTable.Name)?.NewName;
        else if (!string.IsNullOrEmpty(schema.TableRegexPattern) && schema.TablePatternReplaceWith != null) {
            if (primitiveNamingRules.PreserveCasingUsingRegex)
                newTableName = RegexNameReplace(schema.TableRegexPattern, originalTable.Name,
                    schema.TablePatternReplaceWith);
            else
                newTableName = GenerateIdentifier(RegexNameReplace(schema.TableRegexPattern, originalTable.Name,
                    schema.TablePatternReplaceWith));
        } else
            newTableName = base.GenerateCandidateIdentifier(originalTable);

        if (string.IsNullOrWhiteSpace(newTableName)) {
            candidateStringBuilder.Append(GenerateIdentifier(originalTable.Name));

            return candidateStringBuilder.ToString();
        }

        candidateStringBuilder.Append(newTableName);

        return candidateStringBuilder.ToString();
    }

    /// <summary> Name that column </summary>
    public override string GenerateCandidateIdentifier(DatabaseColumn originalColumn) {
        if (originalColumn is null) throw new ArgumentNullException(nameof(originalColumn));

        var candidateStringBuilder = new StringBuilder();

        var schema = GetSchemaReference(originalColumn.Table.Schema);

        if (schema == null || schema.Tables == null) return base.GenerateCandidateIdentifier(originalColumn);

        var renamers = schema.Tables
            .Where(t => t.Name == originalColumn.Table.Name && t.Columns != null)
            .Select(t => t)
            .ToList();

        if (renamers.Count > 0) {
            var column = renamers
                .SelectMany(c => c.Columns.Where(n => n.Name == originalColumn.Name))
                .FirstOrDefault();

            if (column != null) {
                candidateStringBuilder.Append(column.NewName);
                return candidateStringBuilder.ToString();
            }
        }

        var newColumnName = string.Empty;

        if (!string.IsNullOrEmpty(schema.ColumnRegexPattern) && schema.ColumnPatternReplaceWith != null) {
            if (primitiveNamingRules.PreserveCasingUsingRegex)
                newColumnName = RegexNameReplace(schema.ColumnRegexPattern, originalColumn.Name,
                    schema.ColumnPatternReplaceWith);
            else
                newColumnName = GenerateIdentifier(RegexNameReplace(schema.ColumnRegexPattern, originalColumn.Name,
                    schema.ColumnPatternReplaceWith));

            if (!string.IsNullOrWhiteSpace(newColumnName)) {
                candidateStringBuilder.Append(newColumnName);
                return candidateStringBuilder.ToString();
            }
        }

        return base.GenerateCandidateIdentifier(originalColumn);
    }

    /// <summary> Name that navigation dependent </summary>
    public override string GetDependentEndCandidateNavigationPropertyName(IReadOnlyForeignKey foreignKey) {
        if (foreignKey is null) throw new ArgumentNullException(nameof(foreignKey));

        var defaultEfName = base.GetDependentEndCandidateNavigationPropertyName(foreignKey);
        return GetCandidateNavigationPropertyName(foreignKey, false, defaultEfName);
    }

    /// <summary> Name that navigation principal </summary>
    public override string GetPrincipalEndCandidateNavigationPropertyName(IReadOnlyForeignKey foreignKey,
        string dependentEndNavigationPropertyName) {
        if (foreignKey is null) throw new ArgumentNullException(nameof(foreignKey));
        var defaultEfName = base.GetPrincipalEndCandidateNavigationPropertyName(foreignKey, dependentEndNavigationPropertyName);
        return GetCandidateNavigationPropertyName(foreignKey, true, defaultEfName);
    }


    #region internal members

    /// <summary> Name that navigation dependent </summary>
    protected virtual string GetCandidateNavigationPropertyName(IReadOnlyForeignKey foreignKey, bool thisIsPrincipal,
        string defaultEfName) {
        if (foreignKey is null) throw new ArgumentNullException(nameof(foreignKey));
        if (defaultEfName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(defaultEfName));

        var fkName = foreignKey.GetConstraintName();
        var navigation = foreignKey.GetNavigation(!thisIsPrincipal);
        var entity = navigation?.DeclaringEntityType;

        if (entity == null) return defaultEfName;

        var classRef = GetClassReference(entity.Name);

        if (classRef == null || classRef.Properties.IsNullOrEmpty())
            return defaultEfName;
        var navigationRenames = classRef.Properties
            .Where(t => t.FkName == fkName)
            .ToList();

        if (navigationRenames.Count == 0) {
            // Maybe FkName is not defined?  if not, try to locate by expected target name instead
            var someFkNamesEmpty = classRef.Properties.Any(o => o.FkName.IsNullOrWhiteSpace());
            if (!someFkNamesEmpty) {
                // Fk names ARE defined, this property is just not found. Use default.
                return defaultEfName;
            }

            navigationRenames = classRef.Properties.Where(o => o.Name?.Count > 0 && o.Name.Contains(defaultEfName))
                .ToList();
            if (navigationRenames.Count == 0) return defaultEfName;
        }

        // we have candidate matches (by fk name or expected target name). we may need to narrow further.
        if (navigationRenames.Count > 1) {
            if (foreignKey.IsManyToMany()) {
                // many-to-many relationships always set IsPrincipal=true for both ends in the rule file.
                navigationRenames = navigationRenames.Where(o => o.IsPrincipal).ToList();
            } else {
                // filter for this end only
                navigationRenames = navigationRenames.Where(o => o.IsPrincipal == thisIsPrincipal).ToList();
            }
        }

        if (navigationRenames.Count != 1) return defaultEfName;

        var rename = navigationRenames[0];
        if (rename.NewName.HasNonWhiteSpace()) return rename.NewName.Trim();
        return defaultEfName;
    }

    /// <summary>
    /// Convert DB element name to entity identifier. This is the EF Core standard.
    /// Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal.CandidateNamingService.GenerateCandidateIdentifier()
    /// </summary>
    protected virtual string GenerateIdentifier(string value) => value.GenerateCandidateIdentifier();

    /// <summary> Apply regex replace rule to name. </summary>
    protected virtual string RegexNameReplace(string pattern, string originalName, string replacement,
        int timeout = 100) {
        var newName = string.Empty;

        try {
            newName = Regex.Replace(originalName, pattern, replacement, RegexOptions.None,
                TimeSpan.FromMilliseconds(timeout));
        } catch (RegexMatchTimeoutException) {
            Console.WriteLine(
                $"Regex pattern {pattern} time out when trying to match {originalName}, name won't be replaced");
        }

        return newName;
    }

    /// <summary> Return the primitive naming rules SchemaReference object for the given schema name </summary>
    protected virtual SchemaReference GetSchemaReference(string originalSchema)
        => primitiveNamingRules?
            .Schemas
            .FirstOrDefault(x => x.SchemaName == originalSchema);

    /// <summary> Return the navigation naming rules ClassReference object for the given class name </summary>
    protected virtual ClassReference GetClassReference(string tableName)
        => navigationRules?
            .Classes
            .FirstOrDefault(x => x.Name == tableName);

    #endregion
}