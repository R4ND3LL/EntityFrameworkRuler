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
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Design;
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
    private readonly IMessageLogger reporter;
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;
    private readonly IRuleModelUpdater ruleModelUpdater;
    private readonly ICandidateNamingService candidateNamingService;
    private readonly IPluralizer pluralizer;
    private readonly ICSharpUtilities cSharpUtilities;
    private DbContextRule dbContextRule;
    private readonly RelationalScaffoldingModelFactory proxy;
    private readonly MethodInfo visitForeignKeyMethod;
    private readonly MethodInfo addNavigationPropertiesMethod;
    private readonly MethodInfo getEntityTypeNameMethod;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected readonly HashSet<string> OmittedTables = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected readonly HashSet<string> OmittedSchemas = new();

    private CSharpNamer<DatabaseTable> tableNamer;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledRelationalScaffoldingModelFactory(IServiceProvider serviceProvider,
        IMessageLogger reporter,
        IDesignTimeRuleLoader designTimeRuleLoader,
        IRuleModelUpdater ruleModelUpdater,
        ICandidateNamingService candidateNamingService,
        IPluralizer pluralizer,
        ICSharpUtilities cSharpUtilities) {
        this.reporter = reporter;
        this.designTimeRuleLoader = designTimeRuleLoader;
        this.ruleModelUpdater = ruleModelUpdater;
        this.candidateNamingService = candidateNamingService;
        this.pluralizer = pluralizer;
        this.cSharpUtilities = cSharpUtilities;

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
    public IModel Create(DatabaseModel databaseModel, ModelReverseEngineerOptions options) {
        var model = proxy.Create(databaseModel, options);
        ruleModelUpdater?.OnModelCreated(model);
        return model;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual IModel Create(DatabaseModel databaseModel, ModelReverseEngineerOptions options,
        Func<DatabaseModel, ModelReverseEngineerOptions, IModel> baseCall) {
        Func<DatabaseTable, (string, bool)> tableGenAction;

        if (candidateNamingService is RuledCandidateNamingService ruledNamer)
            tableGenAction = t => ruledNamer.GenerateCandidateIdentifierAndIndicateFrozen(t);
        else tableGenAction = t => (candidateNamingService.GenerateCandidateIdentifier(t), false);

        tableNamer = new RuledCSharpUniqueNamer<DatabaseTable>(
            options.UseDatabaseNames
                ? t => (t.Name, false)
                : tableGenAction,
            cSharpUtilities,
            options.NoPluralize
                ? null
                : pluralizer.Singularize);


        return baseCall(databaseModel, options);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual TypeScaffoldingInfo GetTypeScaffoldingInfo(DatabaseColumn column, Func<TypeScaffoldingInfo> baseCall) {
        var typeScaffoldingInfo = baseCall();
        dbContextRule ??= designTimeRuleLoader?.GetDbContextRules() ?? DbContextRule.DefaultNoRulesFoundBehavior;

        if (!TryResolveRuleFor(column, out var schemaRule, out var tableRule, out var columnRule)) return typeScaffoldingInfo;
        if (columnRule?.NewType.HasNonWhiteSpace() != true) return typeScaffoldingInfo;

        var clrType = designTimeRuleLoader?.TryResolveType(columnRule.NewType, typeScaffoldingInfo?.ClrType, reporter);
        if (clrType == null) return typeScaffoldingInfo;
        reporter?.WriteVerbose(
            $"RULED: Property {schemaRule.SchemaName}.{tableRule.Name}.{columnRule.PropertyName} type set to {clrType.FullName}");
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

        var includeTable = dbContextRule.CanIncludeTable(schemaRule, tableRule, table is DatabaseView, out var includeSchema);
        if (!includeTable) {
            if (!includeSchema && OmittedSchemas.Add(table.Schema))
                reporter?.WriteInformation($"RULED: Schema {table.Schema} omitted.");

            OmittedTables.Add(table.GetFullName());
            if (includeSchema) reporter?.WriteInformation($"RULED: Table {table.Schema}.{table.Name} omitted.");

            return null;
        }

        if (tableRule == null) return baseCall(); // no further customization. include default table shape

        var excludedColumns = new HashSet<DatabaseColumn>();

        if (tableRule.Columns?.Count > 0) // remove any NotMapped columns
            foreach (var column in tableRule.Columns.Where(o => o.NotMapped)) {
                var columnToRemove = table.Columns.FirstOrDefault(c => c.Name.EqualsIgnoreCase(column.Name));
                if (columnToRemove == null) continue;
                excludedColumns.Add(columnToRemove);
                table.Columns.Remove(columnToRemove);
            }

        if (tableRule.Columns?.Count > 0 && !tableRule.IncludeUnknownColumns) {
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
            var schemaReference = schemas.FirstOrDefault(o => o.SchemaName == schema);
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
        //return (string)getEntityTypeNameMethod?.Invoke(proxy, new object[] { table });
        return tableNamer.GetName(table);
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
            case "Create" when invocation.Arguments.Length == 2 && invocation.Arguments[0] is DatabaseModel dm &&
                               invocation.Arguments[1] is ModelReverseEngineerOptions op: {
                IModel BaseCall(DatabaseModel databaseModel, ModelReverseEngineerOptions options) {
                    invocation.SetArgumentValue(1, databaseModel);
                    invocation.SetArgumentValue(2, options);
                    invocation.Proceed();
                    return (IModel)invocation.ReturnValue;
                }

                var response = Create(dm, op, BaseCall);
                invocation.ReturnValue = response;
                break;
            }
            case "GetEntityTypeName" when invocation.Arguments.Length == 1 && invocation.Arguments[0] is DatabaseTable t: {
                string BaseCall(DatabaseTable t1) {
                    invocation.SetArgumentValue(1, t1);
                    invocation.Proceed();
                    return (string)invocation.ReturnValue;
                }

                var response = GetEntityTypeName(t);
                invocation.ReturnValue = response;
                break;
            }
            default:
                invocation.Proceed();
                break;
        }
    }
}