using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Rules;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <summary> Naming service override to be used by Ef scaffold process.
/// This will apply custom table, column, and navigation names. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuledCandidateNamingService : CandidateNamingService {
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;
    private readonly IMessageLogger logger;

    private DbContextRule dbContextRule;

    //private NavigationNamingRules navigationRules;
    private readonly MethodInfo generateCandidateIdentifierMethod;

    /// <inheritdoc />
    public RuledCandidateNamingService(IDesignTimeRuleLoader designTimeRuleLoader, IMessageLogger logger) {
        this.designTimeRuleLoader = designTimeRuleLoader;
        this.logger = logger ?? new ConsoleMessageLogger();

        // public virtual string GenerateCandidateIdentifier(string originalIdentifier)
        generateCandidateIdentifierMethod = typeof(CandidateNamingService).GetMethod<string>("GenerateCandidateIdentifier");
        if (generateCandidateIdentifierMethod == null) {
            // this method is expected to be missing for EF 6.  If not EF 6, log a warning
            if (designTimeRuleLoader.EfVersion?.Major != 6)
                this.logger.WriteWarning("Method not found: CandidateNamingService.GenerateCandidateIdentifier(string)");
        }
    }

    private readonly Dictionary<string, NamedTableState> namedTablesByName = new();
    private readonly Dictionary<DatabaseTable, NamedTableState> namedTablesByTable = new();

    /// <summary> Name that table </summary>
    public override string GenerateCandidateIdentifier(DatabaseTable table) {
        if (table == null) throw new ArgumentException("Argument is empty", nameof(table));

        return namedTablesByTable.GetOrAddNew(table, NameTableFactory).Name;
    }

    /// <summary> Name that table, also return indicator whether the name is frozen (explicitly set by user, cannot be altered) </summary>
    public (string, bool) GenerateCandidateIdentifierAndIndicateFrozen(DatabaseTable table) {
        if (table == null) throw new ArgumentException("Argument is empty", nameof(table));

        var state = namedTablesByTable.GetOrAddNew(table, NameTableFactory);
        return (state.Name, state.IsFrozen);
    }

    private NamedTableState NameTableFactory(DatabaseTable table) {
        dbContextRule ??= ResolveDbContextRule();

        if (table.Name == "TargetLens") Debugger.Break();

        if (!dbContextRule.TryResolveRuleFor(table.Schema, table.Name, out var schema, out var tableRule)) {
            var state = NameToState(base.GenerateCandidateIdentifier(table));
            logger?.WriteVerbose($"RULED: Table {table.Schema}.{table.Name} not found in rule file. Auto-named {state.Name}");
            return state;
        }

        if (tableRule?.NewName.HasNonWhiteSpace() == true) {
            var state = NameToState(tableRule.NewName);
            state.IsFrozen = true; // explicitly set by user, cannot be altered by pluralizer
            logger?.WriteVerbose($"RULED: Table {table.Schema}.{table.Name} mapped to entity name {state.Name}");
            return state;
        }

        var candidateStringBuilder = new StringBuilder();
        if (schema.UseSchemaName) candidateStringBuilder.Append(GenerateIdentifier(table.Schema));
        bool usedRegex;
        string newTableName;
        if (!string.IsNullOrEmpty(schema.TableRegexPattern) && schema.TablePatternReplaceWith != null) {
            if (dbContextRule.PreserveCasingUsingRegex)
                newTableName = RegexNameReplace(schema.TableRegexPattern, table.Name,
                    schema.TablePatternReplaceWith);
            else
                newTableName = GenerateIdentifier(RegexNameReplace(schema.TableRegexPattern, table.Name,
                    schema.TablePatternReplaceWith));
            usedRegex = true;
        } else {
            usedRegex = false;
            newTableName = base.GenerateCandidateIdentifier(table);
        }

        if (string.IsNullOrWhiteSpace(newTableName)) {
            candidateStringBuilder.Append(GenerateIdentifier(table.Name));

            return NameToState(candidateStringBuilder.ToString());
        }

        candidateStringBuilder.Append(newTableName);

        var state2 = NameToState(candidateStringBuilder.ToString());
        logger?.WriteVerbose(
            $"RULED: Table {table.Schema}.{table.Name} auto-named entity {state2.Name}{(usedRegex ? " using regex" : "")}");
        return state2;

        NamedTableState NameToState(string newName) {
            var existing = namedTablesByName.TryGetValue(newName);
            if (existing != null) {
                if (existing.Table == table) return existing; // redundant call with same table.

                var distinctName = newName.GetUniqueString(n => namedTablesByName.ContainsKey(n));
                var state = new NamedTableState(distinctName, table, tableRule);
                namedTablesByName.Add(state.Name, state);
                logger?.WriteWarning($"RULED: Name collision detected for entity {newName}.  Enumerated distinct name {distinctName}.");
                return state;
            } else {
                var state = new NamedTableState(newName, table, tableRule);
                namedTablesByName.Add(state.Name, state);
                return state;
            }
        }
    }

    private DbContextRule ResolveDbContextRule() {
        var rule = designTimeRuleLoader?.GetDbContextRules() ?? DbContextRule.DefaultNoRulesFoundBehavior;
        logger?.WriteVerbose(
            $"Candidate Naming Service resolved DB Context Rules for {rule.Name ?? "No Name"} with {rule.Schemas.Count} schemas");
        return rule;
    }

    /// <summary> Name that column </summary>
    [SuppressMessage("ReSharper", "InvertIf")]
    public override string GenerateCandidateIdentifier(DatabaseColumn column) {
        if (column is null) throw new ArgumentNullException(nameof(column));
        dbContextRule ??= ResolveDbContextRule();

        if (!dbContextRule.TryResolveRuleFor(column.Table.Schema, column.Table.Name, out var schema, out var tableRule))
            return base.GenerateCandidateIdentifier(column);
        if (!tableRule.TryResolveRuleFor(column.Name, out var columnRule))
            return base.GenerateCandidateIdentifier(column);

        if (columnRule?.NewName.HasNonWhiteSpace() == true) {
            logger?.WriteVerbose(
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
        dbContextRule ??= ResolveDbContextRule();

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

        logger?.WriteVerbose($"RULED: Navigation {entity.Name}.{rename.NewName} defined");
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

[DebuggerDisplay("{Name}")]
internal class NamedTableState {
    public NamedTableState(string name, DatabaseTable table, TableRule tableRule) {
        Name = name;
        Table = table;
        TableRule = tableRule;
    }

    public string Name { get; set; }
    public DatabaseTable Table { get; set; }
    public TableRule TableRule { get; set; }
    public bool IsFrozen { get; set; }

    /// <summary> implicitly convert to string </summary>
    public static implicit operator string(NamedTableState o) => o.ToString();

    public override string ToString() => Name;
}