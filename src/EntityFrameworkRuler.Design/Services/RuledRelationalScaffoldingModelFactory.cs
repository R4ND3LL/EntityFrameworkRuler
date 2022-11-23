using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkRuler.Design.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Castle.DynamicProxy;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata;
using IInterceptor = Castle.DynamicProxy.IInterceptor;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <summary>
/// This override will apply custom property type mapping to the generated entities.
/// It is also possible to remove columns at this level.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuledRelationalScaffoldingModelFactory : IScaffoldingModelFactory, IInterceptor {
    private readonly IOperationReporter reporter;
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;
    private DbContextRule dbContextRule;
    private readonly RelationalScaffoldingModelFactory proxy;
    private readonly MethodInfo visitForeignKeyMethod;
    private readonly MethodInfo addNavigationPropertiesMethod;
    private readonly MethodInfo getEntityTypeNameMethod;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected readonly HashSet<string> OmittedTables = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected readonly HashSet<string> OmittedSchemas = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledRelationalScaffoldingModelFactory(IServiceProvider serviceProvider,
        IOperationReporter reporter,
        IDesignTimeRuleLoader designTimeRuleLoader) {
        this.reporter = reporter;
        this.designTimeRuleLoader = designTimeRuleLoader;

        // avoid runtime binding errors against EF6 by using reflection and a proxy to access the resources we need.
        // this allows more fluid compatibility with EF versions without retargeting this project.

        try {
            proxy = serviceProvider.CreateClassProxy<RelationalScaffoldingModelFactory>(this);
        } catch (Exception ex) {
            reporter?.WriteError($"Error creating proxy of RelationalScaffoldingModelFactory: {ex.Message}");
            throw;
        }

        var t = typeof(RelationalScaffoldingModelFactory);
        // protected virtual IMutableForeignKey? VisitForeignKey(ModelBuilder modelBuilder,DatabaseForeignKey foreignKey)
        visitForeignKeyMethod = t.GetMethod<ModelBuilder, DatabaseForeignKey>("VisitForeignKey");
        // protected virtual void AddNavigationProperties(IMutableForeignKey foreignKey)
        addNavigationPropertiesMethod = t.GetMethod<IMutableForeignKey>("AddNavigationProperties");
        // protected virtual string GetEntityTypeName(DatabaseTable table)
        getEntityTypeNameMethod = t.GetMethod<DatabaseTable>("GetEntityTypeName");
        if (visitForeignKeyMethod == null)
            reporter?.WriteWarning("Method not found: RelationalScaffoldingModelFactory.VisitForeignKey()");
        if (addNavigationPropertiesMethod == null)
            reporter?.WriteWarning("Method not found: RelationalScaffoldingModelFactory.AddNavigationProperties()");
    }

    /// <inheritdoc />
    public virtual IModel Create(DatabaseModel databaseModel, ModelReverseEngineerOptions options) {
        return proxy.Create(databaseModel, options);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual TypeScaffoldingInfo GetTypeScaffoldingInfo(DatabaseColumn column, Func<TypeScaffoldingInfo> baseCall) {
        var typeScaffoldingInfo = baseCall();
        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? DbContextRule.DefaultNoRulesFoundBehavior;

        if (!TryResolveRuleFor(column, out var schemaRule, out var tableRule, out var columnRule)) return typeScaffoldingInfo;
        if (columnRule?.NewType.HasNonWhiteSpace() != true) return typeScaffoldingInfo;

        var clrType = designTimeRuleLoader?.TryResolveType(columnRule.NewType, typeScaffoldingInfo?.ClrType, reporter);
        if (clrType == null) return typeScaffoldingInfo;
        reporter?.WriteVerbosely(
            $"RULED: Property {schemaRule.Name}.{tableRule.Name}.{columnRule.PropertyName} type set to {clrType.FullName}");
        // Regenerate the TypeScaffoldingInfo based on our new CLR type.
        return typeScaffoldingInfo.WithType(clrType);
    }

    /// <summary> Get the type changing rule for this column </summary>
    protected virtual bool TryResolveRuleFor(DatabaseColumn column, out SchemaRule schemaRule, out TableRule tableRule,
        out ColumnRule columnRule) {
        return dbContextRule.TryResolveRuleFor(column?.Table?.Schema, column?.Table?.Name, column?.Name,
            out schemaRule, out tableRule, out columnRule);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual EntityTypeBuilder VisitTable(ModelBuilder modelBuilder, DatabaseTable table, Func<EntityTypeBuilder> baseCall) {
        // ReSharper disable once AssignNullToNotNullAttribute
        if (table is null) return baseCall();

        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? DbContextRule.DefaultNoRulesFoundBehavior;

        dbContextRule.TryResolveRuleFor(table.Schema, table.Name, out var schemaRule, out var tableRule);

        if (schemaRule == null) {
            if (dbContextRule == null || dbContextRule.IncludeUnknownSchemas) return baseCall(); // nothing to go on

            if (OmittedSchemas.Add(table.Schema))
                reporter?.WriteInformation($"RULED: Schema {table.Schema} omitted.");
            OmittedTables.Add(table.GetFullName());
            return null; // alien schema. do not generate unknown
        }

        var isView = table is DatabaseView;
        var includeTable =
            schemaRule.Mapped &&
            (
                (tableRule == null && (isView ? schemaRule.IncludeUnknownViews : schemaRule.IncludeUnknownTables))
                ||
                (tableRule?.Mapped == true)
            );

        // drop the table if all columns are not mapped
        if (includeTable && tableRule?.IncludeUnknownColumns == false &&
            (tableRule.Columns.IsNullOrEmpty() || tableRule.Columns.All(o => o.NotMapped))) includeTable = false;

        var excludedColumns = new HashSet<DatabaseColumn>();

        if (!includeTable) {
            // remove ALL columns
            foreach (var columnToRemove in table.Columns) excludedColumns.Add(columnToRemove);
            excludedColumns.ForAll(o => table.Columns.Remove(o));
        } else if (tableRule?.Columns?.Count > 0) // remove any NotMapped columns
            foreach (var column in tableRule.Columns.Where(o => o.NotMapped)) {
                var columnToRemove = table.Columns.FirstOrDefault(c => c.Name.EqualsIgnoreCase(column.Name));
                if (columnToRemove == null) continue;
                excludedColumns.Add(columnToRemove);
                table.Columns.Remove(columnToRemove);
            }

        if (includeTable && tableRule?.Columns?.Count > 0 && !tableRule.IncludeUnknownColumns) {
            // remove any unknown columns
            foreach (var columnToRemove in table.Columns
                         .Where(o => tableRule.Columns
                             .FirstOrDefault(c => c.Name.EqualsIgnoreCase(o.Name))?.Mapped != true)
                         .ToList()) {
                excludedColumns.Add(columnToRemove);
                table.Columns.Remove(columnToRemove);
            }
        }

        if (excludedColumns.Count > 0) {
            var indexesToBeRemoved = new HashSet<DatabaseIndex>();
            foreach (var index in table.Indexes)
            foreach (var column in index.Columns)
                if (excludedColumns.Contains(column)) {
                    indexesToBeRemoved.Add(index);
                    break;
                }

            foreach (var index in indexesToBeRemoved) table.Indexes.Remove(index);

            var fksToBeRemoved = new HashSet<DatabaseForeignKey>();
            foreach (var foreignKey in table.ForeignKeys)
            foreach (var column in foreignKey.Columns)
                if (excludedColumns.Contains(column)) {
                    fksToBeRemoved.Add(foreignKey);
                    break;
                }

            foreach (var index in fksToBeRemoved) table.ForeignKeys.Remove(index);
        }

        if (table.ForeignKeys.Count > 0 && OmittedTables.Count > 0) {
            // check to see if any of the foreign keys map to omitted tables. if so, nuke them.
            var fksToBeRemoved = new HashSet<DatabaseForeignKey>();
            foreach (var foreignKey in table.ForeignKeys)
                if (OmittedTables.Contains(foreignKey.PrincipalTable.GetFullName()))
                    fksToBeRemoved.Add(foreignKey);

            foreach (var index in fksToBeRemoved) table.ForeignKeys.Remove(index);
        }

        if (table.Columns.Count == 0 && getEntityTypeNameMethod != null) {
            // remove the entire table
            OmittedTables.Add(table.GetFullName());
            table.Indexes.Clear();
            table.ForeignKeys.Clear();
            var entityTypeName = GetEntityTypeName(table);
            modelBuilder.Model.RemoveEntityType(entityTypeName);
            reporter?.WriteInformation($"RULED: Table {table.Schema}.{table.Name} omitted.");
            return null;
        }

        foreach (var excludedColumn in excludedColumns)
            reporter?.WriteInformation($"RULED: Column {table.Schema}.{table.Name}.{excludedColumn.Name} omitted.");
        return baseCall();
    }


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual ModelBuilder VisitForeignKeys(ModelBuilder modelBuilder, IList<DatabaseForeignKey> foreignKeys,
        Func<IList<DatabaseForeignKey>, ModelBuilder> baseCall) {
        if (visitForeignKeyMethod == null || addNavigationPropertiesMethod == null) return baseCall(foreignKeys);

        ArgumentNullException.ThrowIfNull(foreignKeys);
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var schemaNames = foreignKeys.Select(o => o.Table?.Schema).Where(o => o.HasNonWhiteSpace()).Distinct().ToArray();

        var schemas = schemaNames.Select(o => dbContextRule?.TryResolveRuleFor(o))
            .Where(o => o?.UseManyToManyEntity == true).ToArray();

        if (OmittedTables.Count > 0) {
            // check to see if the foreign key maps to an omitted table. if so, nuke it.
            var fksToBeRemoved = new HashSet<DatabaseForeignKey>();
            foreach (var foreignKey in foreignKeys)
                if (OmittedTables.Contains(foreignKey.PrincipalTable.GetFullName()))
                    fksToBeRemoved.Add(foreignKey);
                else if (OmittedSchemas.Contains(foreignKey.PrincipalTable.Schema))
                    fksToBeRemoved.Add(foreignKey);

            foreignKeys = foreignKeys.Where(o => !fksToBeRemoved.Contains(o)).ToList();
        }

        if (schemas.IsNullOrEmpty()) return baseCall(foreignKeys);

        foreach (var grp in foreignKeys.GroupBy(o => o.Table?.Schema)) {
            var schema = grp.Key;
            var schemaForeignKeys = grp.ToArray();
            var schemaReference = schemas.FirstOrDefault(o => o.Name == schema);
            if (schemaReference == null) {
                modelBuilder = baseCall(schemaForeignKeys);
                continue;
            }

            // force simple ManyToMany junctions to be generated as entities
            reporter?.WriteInformation($"RULED: Simple many-to-many junctions in {schema} are being forced to generate entities.");
            foreach (var fk in schemaForeignKeys)
                VisitForeignKey(modelBuilder, fk);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            foreach (var foreignKey in entityType.GetForeignKeys())
                AddNavigationProperties(foreignKey);
        }

        return modelBuilder;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual void VisitForeignKey(ModelBuilder modelBuilder, DatabaseForeignKey fk) {
        visitForeignKeyMethod!.Invoke(proxy, new object[] { modelBuilder, fk });
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual void AddNavigationProperties(IMutableForeignKey foreignKey) {
        addNavigationPropertiesMethod!.Invoke(proxy, new object[] { foreignKey });
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual string GetEntityTypeName(DatabaseTable table) {
        return (string)getEntityTypeNameMethod?.Invoke(proxy, new object[] { table });
    }

    void IInterceptor.Intercept(IInvocation invocation) {
        switch (invocation.Method.Name) {
            case "GetTypeScaffoldingInfo" when invocation.Arguments.Length == 1 && invocation.Arguments[0] is DatabaseColumn dc: {
                TypeScaffoldingInfo BaseCall() {
                    invocation.Proceed();
                    return (TypeScaffoldingInfo)invocation.ReturnValue;
                }

                var response = GetTypeScaffoldingInfo(dc, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "VisitTable" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is ModelBuilder mb &&
                                   invocation.Arguments[1] is DatabaseTable dt: {
                EntityTypeBuilder BaseCall() {
                    invocation.Proceed();
                    return (EntityTypeBuilder)invocation.ReturnValue;
                }

                var response = VisitTable(mb, dt, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "VisitForeignKeys" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is ModelBuilder mb &&
                                         invocation.Arguments[1] is IList<DatabaseForeignKey> fks: {
                ModelBuilder BaseCall(IList<DatabaseForeignKey> databaseForeignKeys) {
                    invocation.SetArgumentValue(1, databaseForeignKeys);
                    invocation.Proceed();
                    return (ModelBuilder)invocation.ReturnValue;
                }

                var response = VisitForeignKeys(mb, fks, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            default:
                invocation.Proceed();
                break;
        }
    }
}