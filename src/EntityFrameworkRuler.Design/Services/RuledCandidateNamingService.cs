using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <summary> Naming service override to be used by Ef scaffold process.
/// This will apply custom table, column, and navigation names. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuledCandidateNamingService : CandidateNamingService {
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;
    private readonly IOperationReporter reporter;

    private DbContextRule dbContextRule;

    //private NavigationNamingRules navigationRules;
    private readonly MethodInfo generateCandidateIdentifierMethod;

    /// <inheritdoc />
    public RuledCandidateNamingService(IDesignTimeRuleLoader designTimeRuleLoader, IOperationReporter reporter) {
        this.designTimeRuleLoader = designTimeRuleLoader;
        this.reporter = reporter;

        // public virtual string GenerateCandidateIdentifier(string originalIdentifier)
        generateCandidateIdentifierMethod = typeof(CandidateNamingService).GetMethod<string>("GenerateCandidateIdentifier");
        if (generateCandidateIdentifierMethod == null)
            reporter?.WriteWarning("Method not found: CandidateNamingService.GenerateCandidateIdentifier(string)");
    }

    /// <summary> Name that table </summary>
    public override string GenerateCandidateIdentifier(DatabaseTable table) {
        if (table == null) throw new ArgumentException("Argument is empty", nameof(table));
        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? DbContextRule.DefaultNoRulesFoundBehavior;


        if (!dbContextRule.TryResolveRuleFor(table.Schema, table.Name, out var schema, out var tableRule))
            return base.GenerateCandidateIdentifier(table);

        if (tableRule?.NewName.HasNonWhiteSpace() == true) {
            reporter?.WriteVerbosely($"RULED: Table {table.Schema}.{table.Name} mapped to entity name {tableRule.NewName}");
            return tableRule.NewName;
        }

        var candidateStringBuilder = new StringBuilder();
        if (schema.UseSchemaName) candidateStringBuilder.Append(GenerateIdentifier(table.Schema));

        string newTableName;
        if (!string.IsNullOrEmpty(schema.TableRegexPattern) && schema.TablePatternReplaceWith != null) {
            if (dbContextRule.PreserveCasingUsingRegex)
                newTableName = RegexNameReplace(schema.TableRegexPattern, table.Name,
                    schema.TablePatternReplaceWith);
            else
                newTableName = GenerateIdentifier(RegexNameReplace(schema.TableRegexPattern, table.Name,
                    schema.TablePatternReplaceWith));
        } else
            newTableName = base.GenerateCandidateIdentifier(table);

        if (string.IsNullOrWhiteSpace(newTableName)) {
            candidateStringBuilder.Append(GenerateIdentifier(table.Name));

            return candidateStringBuilder.ToString();
        }

        candidateStringBuilder.Append(newTableName);

        return candidateStringBuilder.ToString();
    }

    /// <summary> Name that column </summary>
    [SuppressMessage("ReSharper", "InvertIf")]
    public override string GenerateCandidateIdentifier(DatabaseColumn column) {
        if (column is null) throw new ArgumentNullException(nameof(column));
        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? DbContextRule.DefaultNoRulesFoundBehavior;

        if (!dbContextRule.TryResolveRuleFor(column?.Table?.Schema, column?.Table?.Name, out var schema, out var tableRule))
            return base.GenerateCandidateIdentifier(column);
        if (!tableRule.TryResolveRuleFor(column?.Name, out var columnRule))
            return base.GenerateCandidateIdentifier(column);

        if (columnRule?.NewName.HasNonWhiteSpace() == true) {
            reporter?.WriteVerbosely(
                $"RULED: Column {column.Table.Schema}.{column.Table.Name}.{columnRule.Name} property name set to {columnRule.NewName}");
            return columnRule.NewName;
        }

        if (!string.IsNullOrEmpty(schema.ColumnRegexPattern) && schema.ColumnPatternReplaceWith != null) {
            var candidateStringBuilder = new StringBuilder();
            string newColumnName;
            if (dbContextRule.PreserveCasingUsingRegex)
                newColumnName = RegexNameReplace(schema.ColumnRegexPattern, column.Name,
                    schema.ColumnPatternReplaceWith);
            else
                newColumnName = GenerateIdentifier(RegexNameReplace(schema.ColumnRegexPattern, column.Name,
                    schema.ColumnPatternReplaceWith));

            if (!string.IsNullOrWhiteSpace(newColumnName)) {
                candidateStringBuilder.Append(newColumnName);
                return candidateStringBuilder.ToString();
            }
        }

        return base.GenerateCandidateIdentifier(column);
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

    /// <summary> Name that navigation dependent </summary>
    protected virtual string GetCandidateNavigationPropertyName(IReadOnlyForeignKey foreignKey, bool thisIsPrincipal,
        Func<string> defaultEfName) {
        if (foreignKey is null) throw new ArgumentNullException(nameof(foreignKey));
        if (defaultEfName == null) throw new ArgumentNullException(nameof(defaultEfName));
        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? DbContextRule.DefaultNoRulesFoundBehavior;

        var fkName = foreignKey.GetConstraintName();
        var entity = thisIsPrincipal ? foreignKey.PrincipalEntityType : foreignKey.DeclaringEntityType;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        // ReSharper disable once HeuristicUnreachableCode
        if (entity == null) return defaultEfName();

        string tableName;
        try {
            tableName = entity.GetTableName() ?? entity.GetViewName();
        } catch {
            tableName = null;
        }

        string schemaName;
        try {
            schemaName = entity.GetSchema() ?? entity.GetViewSchema();
        } catch {
            schemaName = null;
        }

        var tableRule = dbContextRule.TryResolveRuleForEntity(entity.Name, schemaName, tableName);
        if (tableRule?.Navigations.IsNullOrEmpty() != false) return defaultEfName();

        var rename = tableRule.TryResolveNavigationRuleFor(fkName, defaultEfName, thisIsPrincipal, foreignKey.IsManyToMany());
        if (rename?.NewName.IsNullOrWhiteSpace() != false) return defaultEfName();

        reporter?.WriteVerbosely($"RULED: Entity {entity.Name} navigation {rename.NewName} defined");
        return rename.NewName.Trim();
    }

    /// <summary>
    /// Convert DB element name to entity identifier. This is the EF Core standard.
    /// Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal.CandidateNamingService.GenerateCandidateIdentifier()
    /// </summary>
    protected virtual string GenerateIdentifier(string value) {
        if (generateCandidateIdentifierMethod != null) {
            // use the actual EF process
            return (string)generateCandidateIdentifierMethod.Invoke(this, new object[] { value });
        }

        // use the EF Ruler emulation of the GenerateCandidateIdentifier
        return value.GenerateCandidateIdentifier();
    }

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

    #endregion
}