using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Rules.PrimitiveNaming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using ICSharpUtilities = Microsoft.EntityFrameworkCore.Scaffolding.Internal.ICSharpUtilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <summary>
/// This override will apply custom property type mapping to the generated entities.
/// It is also possible to remove columns at this level.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class EfRulerRelationalScaffoldingModelFactory : RelationalScaffoldingModelFactory {
    private readonly IOperationReporter reporter;
    private readonly IRuleLoader ruleLoader;
    private PrimitiveNamingRules primitiveNamingRules;

    /// <inheritdoc />
    public EfRulerRelationalScaffoldingModelFactory(
        IOperationReporter reporter,
        ICandidateNamingService candidateNamingService,
        IPluralizer pluralizer,
        ICSharpUtilities cSharpUtilities,
        IScaffoldingTypeMapper scaffoldingTypeMapper,
#if NET6
        LoggingDefinitions loggingDefinitions,
#endif
        IModelRuntimeInitializer modelRuntimeInitializer,
        IRuleLoader ruleLoader)
#if NET6
        : base(reporter, candidateNamingService, pluralizer, cSharpUtilities, scaffoldingTypeMapper, loggingDefinitions,
            modelRuntimeInitializer) {
#elif NET7
        : base(reporter, candidateNamingService, pluralizer, cSharpUtilities, scaffoldingTypeMapper, modelRuntimeInitializer) {
#endif
        this.reporter = reporter;
        this.ruleLoader = ruleLoader;
    }

    /// <inheritdoc />
    protected override TypeScaffoldingInfo GetTypeScaffoldingInfo(DatabaseColumn column) {
        var typeScaffoldingInfo = base.GetTypeScaffoldingInfo(column);
        primitiveNamingRules ??= ruleLoader?.GetPrimitiveNamingRules() ?? new();


        if (!TryResolveRuleFor(column, out _, out var tableRule, out var columnRule)) return typeScaffoldingInfo;
        if (columnRule?.NewType.HasNonWhiteSpace() != true) return typeScaffoldingInfo;

        try {
            var clrTypeName = columnRule.NewType;
            var clrType = ruleLoader?.TryResolveType(clrTypeName, typeScaffoldingInfo?.ClrType);

            if (clrType == null) {
                WriteWarning($"Type not found: {columnRule.NewType}");
                return typeScaffoldingInfo;
            }

            // Regenerate the TypeScaffoldingInfo based on our new CLR type.
            typeScaffoldingInfo = typeScaffoldingInfo.WithType(clrType);
            WriteVerbose($"Column rule applied: {tableRule.Name}.{columnRule.PropertyName} type set to {columnRule.NewType}");
            return typeScaffoldingInfo;
        } catch (Exception ex) {
            WriteWarning($"Error loading type '{columnRule.NewType}' reference: {ex.Message}");
        }

        return typeScaffoldingInfo;
    }


    /// <summary> Get the type changing rule for this column </summary>
    protected virtual bool TryResolveRuleFor(DatabaseColumn column, out SchemaRule schemaRule, out TableRule tableRule,
        out ColumnRule columnRule) {
        return primitiveNamingRules.TryResolveRuleFor(column?.Table?.Schema, column?.Table?.Name, column?.Name,
            out schemaRule, out tableRule, out columnRule);
    }

    /// <inheritdoc />
    protected override EntityTypeBuilder VisitTable(ModelBuilder modelBuilder, DatabaseTable table) {
        // ReSharper disable once AssignNullToNotNullAttribute
        if (table is null) return base.VisitTable(modelBuilder, table);

        primitiveNamingRules ??= ruleLoader?.GetPrimitiveNamingRules() ?? new PrimitiveNamingRules();

        if (!primitiveNamingRules.TryResolveRuleFor(table.Schema, table.Name, out var schemaRule, out var tableRule))
            return base.VisitTable(modelBuilder, table);

        var excludedColumns = new List<DatabaseColumn>();
        if (tableRule?.Columns?.Count > 0)
            foreach (var column in tableRule.Columns.Where(o => o.NotMapped)) {
                var columnToRemove = table.Columns.FirstOrDefault(c => c.Name.Equals(column.Name, StringComparison.OrdinalIgnoreCase));
                if (columnToRemove == null) continue;
                excludedColumns.Add(columnToRemove);
                table.Columns.Remove(columnToRemove);
            }

        if (excludedColumns.Count == 0) return base.VisitTable(modelBuilder, table);

        var indexesToBeRemoved = new List<DatabaseIndex>();
        foreach (var index in table.Indexes)
        foreach (var column in index.Columns)
            if (excludedColumns.Contains(column))
                indexesToBeRemoved.Add(index);

        foreach (var index in indexesToBeRemoved) table.Indexes.Remove(index);

        return base.VisitTable(modelBuilder, table);
    }

#if NET6
    /// <inheritdoc />
    protected override ModelBuilder VisitForeignKeys(ModelBuilder modelBuilder, IList<DatabaseForeignKey> foreignKeys) {
        ArgumentNullException.ThrowIfNull(foreignKeys);

        ArgumentNullException.ThrowIfNull(modelBuilder);

        var schemaNames = foreignKeys.Select(o => o.Table?.Schema).Where(o => o.HasNonWhiteSpace()).Distinct().ToArray();

        var schemas = schemaNames.Select(o => primitiveNamingRules?.TryResolveRuleFor(o))
            .Where(o => o?.UseManyToManyEntity == true).ToArray();

        if (schemas.IsNullOrEmpty()) return base.VisitForeignKeys(modelBuilder, foreignKeys);

        foreach (var grp in foreignKeys.GroupBy(o => o.Table?.Schema)) {
            var schema = grp.Key;
            var schemaForeignKeys = grp.ToArray();
            var schemaReference = schemas.FirstOrDefault(o => o.SchemaName == schema);
            if (schemaReference == null) {
                modelBuilder = base.VisitForeignKeys(modelBuilder, schemaForeignKeys);
                continue;
            }

            // force simple ManyToMany junctions to be generated as entities
            WriteInformation($"{schema} Simple ManyToMany junctions are being forced to generate entities for schema {schema}");
            foreach (var fk in schemaForeignKeys) VisitForeignKey(modelBuilder, fk);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            foreach (var foreignKey in entityType.GetForeignKeys())
                AddNavigationProperties(foreignKey);
        }

        return modelBuilder;
    }
#endif

    internal void WriteWarning(string msg) {
        reporter?.WriteWarning(msg);
        EfRulerCandidateNamingService.DebugLog(msg);
    }

    internal void WriteInformation(string msg) {
        reporter?.WriteInformation(msg);
        EfRulerCandidateNamingService.DebugLog(msg);
    }

    internal void WriteVerbose(string msg) {
        reporter?.WriteVerbose(msg);
        EfRulerCandidateNamingService.DebugLog(msg);
    }
}