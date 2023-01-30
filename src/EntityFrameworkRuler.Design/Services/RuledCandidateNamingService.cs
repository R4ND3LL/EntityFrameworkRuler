using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Design.Services.Models;
using EntityFrameworkRuler.Rules;
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

    private DbContextRuleNode dbContextRule;

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


    /// <summary> Name that table </summary>
    public override string GenerateCandidateIdentifier(DatabaseTable table) {
        if (table == null) throw new ArgumentException("Argument is empty", nameof(table));

#if DEBUG
        if (Debugger.IsAttached) Debugger.Break();
        // Components should be overriden to avoid this method in favor of the state creation method below.
#endif

        var state = GenerateCandidateNameState(table);
        return state.Name;
    }

    /// <summary> Name that table, also return indicator whether the name is frozen (explicitly set by user, cannot be altered) </summary>
    public virtual NamedElementState<DatabaseTable, EntityRule> GenerateCandidateNameState(DatabaseTable table, bool forDbSetName = false) {
        if (table == null) throw new ArgumentException("Argument is empty", nameof(table));
        dbContextRule ??= ResolveDbContextRule();

        EntityRuleNode entityRule = null;
        var entityRules = dbContextRule.TryResolveRuleFor(table);
        if (entityRules.Count == 0) return InvokeBaseCall();
        Debug.Assert(entityRules.Count == 1, "How do we reliably select the correct rule when splitting?");
        entityRule = entityRules.FirstOrDefault(o => o.ShouldMap());
        if (entityRule == null) return InvokeBaseCall();

        if (forDbSetName && entityRule?.Rule.DbSetName.HasNonWhiteSpace() == true) {
            // Name explicitly set by user. cannot be altered by pluralizer so set FROZEN
            var state = NameToState(entityRule.Rule.DbSetName, true);
            logger?.WriteVerbose($"RULED: Table {table.GetFullName()} mapped to DbSet name {state.Name}");
            return state;
        }

        if (entityRule?.Rule.NewName.HasNonWhiteSpace() == true) {
            // Name explicitly set by user. if not naming the DbSet then this cannot be altered by pluralizer so set FROZEN
            var state = NameToState(entityRule.Rule.NewName, !forDbSetName);
            logger?.WriteVerbose(
                $"RULED: Table {table.GetFullName()} mapped to {(forDbSetName ? "DbSet" : "entity")} name {state.Name}");
            return state;
        }

        var schema = entityRule.Parent.Rule;
        var candidateStringBuilder = new StringBuilder();
        if (schema?.UseSchemaName == true && schema.SchemaName.IsNullOrWhiteSpace()) schema.UseSchemaName = false;
        if (schema?.UseSchemaName == true) candidateStringBuilder.Append(GenerateIdentifier(table.Schema));
        bool usedRegex;
        string newTableName;
        if (!string.IsNullOrEmpty(schema.TableRegexPattern) && schema.TablePatternReplaceWith != null) {
            if (dbContextRule.Rule.PreserveCasingUsingRegex)
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
            $"RULED: {(table is DatabaseView ? "View" : "Table")} {table.GetFullName()} auto-named {(forDbSetName ? "DbSet" : "entity")} {state2.Name}{(usedRegex ? " using regex" : "")}");
        return state2;

        NamedElementState<DatabaseTable, EntityRule> NameToState(string newName, bool isFrozen = false) {
            var state = new NamedElementState<DatabaseTable, EntityRule>(newName, table, entityRule, isFrozen);
            return state;
        }

        NamedElementState<DatabaseTable, EntityRule> InvokeBaseCall() {
            var state = NameToState(base.GenerateCandidateIdentifier(table));
            logger?.WriteVerbose($"RULED: Table {table.GetFullName()} not found in rule file. Auto-named {state.Name}");
            return state;
        }
    }

    private DbContextRuleNode ResolveDbContextRule() {
        var rule = designTimeRuleLoader?.GetDbContextRules() ?? new DbContextRuleNode(DbContextRule.DefaultNoRulesFoundBehavior);
        logger?.WriteVerbose(
            $"Candidate Naming Service resolved DB Context Rules for {rule.DbName ?? "No Name"} with {rule.Schemas.Count} schemas");
        return rule;
    }

    /// <summary> Name that column </summary>
    [SuppressMessage("ReSharper", "InvertIf")]
    public override string GenerateCandidateIdentifier(DatabaseColumn column) {
        if (column is null) throw new ArgumentNullException(nameof(column));
        dbContextRule ??= ResolveDbContextRule();

        var entityRules = dbContextRule.TryResolveRuleFor(column.Table);
        if (entityRules.Count == 0)
            return base.GenerateCandidateIdentifier(column);
        var propertyRules = entityRules
            .Select(o => o.TryResolveRuleFor(column.Name))
            .Where(o => o?.ShouldMap() == true).Distinct().ToArray();
        if (propertyRules.Length == 0) return base.GenerateCandidateIdentifier(column);
        //Debug.Assert(propertyRules.Length == 1, "How do we reliably select the correct rule when splitting?");
        var propertyRule = propertyRules.FirstOrDefault(o => o.Rule.NewName.HasNonWhiteSpace()) ?? propertyRules.First();
        if (propertyRule == null) return base.GenerateCandidateIdentifier(column);

        if (propertyRule.Rule.NewName.HasNonWhiteSpace()) {
            logger?.WriteVerbose(
                $"RULED: Column {column.Table.GetFullName()}.{propertyRule.Rule.Name} property name set to {propertyRule.Rule.NewName}");
            return propertyRule.Rule.NewName;
        }

        var schema = propertyRule.Parent.Parent.Rule;
        if (!string.IsNullOrEmpty(schema.ColumnRegexPattern) && schema.ColumnPatternReplaceWith != null) {
            var candidateStringBuilder = new StringBuilder();
            string newColumnName;
            if (dbContextRule.Rule.PreserveCasingUsingRegex)
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

        var fkName = foreignKey.GetConstraintNameForTableOrView();
        var entity = thisIsPrincipal ? foreignKey.PrincipalEntityType : foreignKey.DeclaringEntityType;
        var inverseEntity = thisIsPrincipal ? foreignKey.DeclaringEntityType : foreignKey.PrincipalEntityType;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        // ReSharper disable once HeuristicUnreachableCode
        if (entity == null) return defaultEfName();

        var entityRule = dbContextRule.TryResolveRuleForEntityName(entity.Name);

        if (entityRule == null || !entityRule.GetNavigations().Any()) return defaultEfName();

        var navigationRule = entityRule.TryResolveNavigationRuleFor(fkName, defaultEfName, thisIsPrincipal, foreignKey.IsManyToMany(),
            inverseEntity?.Name);
        if (navigationRule?.Rule?.NewName.IsNullOrWhiteSpace() != false) return defaultEfName();

        logger?.WriteVerbose($"RULED: Navigation {entity.Name}.{navigationRule.Rule.NewName} defined");
        return navigationRule.Rule.NewName.Trim();
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