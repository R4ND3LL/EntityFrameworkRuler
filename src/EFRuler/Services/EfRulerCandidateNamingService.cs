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

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Services;

/// <summary> Naming service override to be used by Ef scaffold process. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class EfRulerCandidateNamingService : CandidateNamingService {
    private readonly IRuleProvider ruleProvider;
    private PrimitiveNamingRules primitiveNamingRules;
    private NavigationNamingRules navigationRules;

    /// <inheritdoc />
    public EfRulerCandidateNamingService(IRuleProvider ruleProvider) {
        this.ruleProvider = ruleProvider;
#if DEBUG
        if (!Debugger.IsAttached) Debugger.Launch();
#endif
    }

    /// <summary> Name that table </summary>
    public override string GenerateCandidateIdentifier(DatabaseTable originalTable) {
        if (originalTable == null) throw new ArgumentException("Argument is empty", nameof(originalTable));
        primitiveNamingRules ??= ruleProvider?.GetPrimitiveNamingRules() ?? new PrimitiveNamingRules();

        var candidateStringBuilder = new StringBuilder();

        var schema = GetSchemaReference(originalTable.Schema);

        if (schema == null) return base.GenerateCandidateIdentifier(originalTable);

        if (schema.UseSchemaName) candidateStringBuilder.Append(GenerateIdentifier(originalTable.Schema));

        var tableRule =
            schema?.Tables.FirstOrDefault(o => o.Name == originalTable.Name) ??
            schema?.Tables.FirstOrDefault(o => o.Name.IsNullOrWhiteSpace() && o.EntityName == originalTable.Name);

        if (tableRule?.NewName.HasNonWhiteSpace() == true) {
            Log($"Table rule applied: {tableRule.Name} to {tableRule.NewName}");
            return tableRule.NewName;
        }

        string newTableName;
        if (!string.IsNullOrEmpty(schema.TableRegexPattern) && schema.TablePatternReplaceWith != null) {
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
    [SuppressMessage("ReSharper", "InvertIf")]
    public override string GenerateCandidateIdentifier(DatabaseColumn originalColumn) {
        if (originalColumn is null) throw new ArgumentNullException(nameof(originalColumn));
        primitiveNamingRules ??= ruleProvider?.GetPrimitiveNamingRules() ?? new PrimitiveNamingRules();

        var schema = GetSchemaReference(originalColumn.Table.Schema);

        var tableRule =
            schema?.Tables?.FirstOrDefault(o => o.Name == originalColumn.Table.Name) ??
            schema?.Tables?.FirstOrDefault(o => o.Name.IsNullOrWhiteSpace() && o.EntityName == originalColumn.Table.Name);

        if (tableRule == null) return base.GenerateCandidateIdentifier(originalColumn);

        var columnRule =
            tableRule?.Columns?.FirstOrDefault(o => o.Name == originalColumn.Name) ??
            tableRule?.Columns?.FirstOrDefault(o => o.Name.IsNullOrWhiteSpace() && o.PropertyName == originalColumn.Name);

        if (columnRule?.NewName.HasNonWhiteSpace() == true) {
            Log($"Column rule applied: {columnRule.Name} to {columnRule.NewName}");
            return columnRule.NewName;
        }

        if (!string.IsNullOrEmpty(schema.ColumnRegexPattern) && schema.ColumnPatternReplaceWith != null) {
            var candidateStringBuilder = new StringBuilder();
            string newColumnName;
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
        string DefaultEfName() => base.GetDependentEndCandidateNavigationPropertyName(foreignKey);
        return GetCandidateNavigationPropertyName(foreignKey, false, DefaultEfName);
    }

    /// <summary> Name that navigation principal </summary>
    public override string GetPrincipalEndCandidateNavigationPropertyName(IReadOnlyForeignKey foreignKey,
        string dependentEndNavigationPropertyName) {
        if (foreignKey is null) throw new ArgumentNullException(nameof(foreignKey));
        string DefaultEfName() => base.GetPrincipalEndCandidateNavigationPropertyName(foreignKey, dependentEndNavigationPropertyName);
        return GetCandidateNavigationPropertyName(foreignKey, true, DefaultEfName);
    }


    #region internal members

    [Conditional("DEBUG")]
    internal static void Log(string msg) => Console.WriteLine(msg);

    /// <summary> Name that navigation dependent </summary>
    protected virtual string GetCandidateNavigationPropertyName(IReadOnlyForeignKey foreignKey, bool thisIsPrincipal,
        Func<string> defaultEfName) {
        if (foreignKey is null) throw new ArgumentNullException(nameof(foreignKey));
        if (defaultEfName == null) throw new ArgumentNullException(nameof(defaultEfName));
        navigationRules ??= ruleProvider?.GetNavigationNamingRules() ?? new NavigationNamingRules();
        //if (defaultEfName.IsNullOrEmpty()) defaultEfName = "_";

        var fkName = foreignKey.GetConstraintName();
        var navigation = foreignKey.GetNavigation(!thisIsPrincipal);
        IReadOnlyEntityType entity = navigation?.DeclaringEntityType??foreignKey.DeclaringEntityType;
        if (entity == null) return defaultEfName();

        string tableName;
        try {
            tableName = entity.GetTableName();
        } catch {
            tableName = null;
        }

        var classRef = GetClassReference(entity.Name, tableName);

        if (classRef == null || classRef.Properties.IsNullOrEmpty())
            return defaultEfName();
        var navigationRenames = classRef.Properties
            .Where(t => t.FkName == fkName)
            .ToList();

        string efName = null;
        if (navigationRenames.Count == 0) {
            // Maybe FkName is not defined?  if not, try to locate by expected target name instead
            var someFkNamesEmpty = classRef.Properties.Any(o => o.FkName.IsNullOrWhiteSpace());
            if (!someFkNamesEmpty) {
                // Fk names ARE defined, this property is just not found. Use default.
                return defaultEfName();
            }

            efName = defaultEfName();
            navigationRenames = classRef.Properties.Where(o => o.Name?.Count > 0 && o.Name.Contains(efName))
                .ToList();
            if (navigationRenames.Count == 0) return efName ?? defaultEfName();
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

        if (navigationRenames.Count != 1) return efName ?? defaultEfName();

        var rename = navigationRenames[0];
        if (rename.NewName.IsNullOrWhiteSpace()) return efName ?? defaultEfName();

        Log($"Navigation rule applied: {rename.Name} to {rename.NewName}");
        return rename.NewName.Trim();
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
        => primitiveNamingRules?.Schemas.FirstOrDefault(x => x.SchemaName == originalSchema);

    /// <summary> Return the navigation naming rules ClassReference object for the given class name </summary>
    protected virtual ClassReference GetClassReference(string entityName, string tableName)
        => navigationRules?.Classes.FirstOrDefault(x => x.Name == entityName) ??
           navigationRules?.Classes.FirstOrDefault(x => x.DbName == tableName);

    #endregion
}