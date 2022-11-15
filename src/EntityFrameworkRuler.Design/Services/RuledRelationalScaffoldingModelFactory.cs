using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Rules.PrimitiveNaming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Castle.DynamicProxy;
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
    private PrimitiveNamingRules primitiveNamingRules;
    private readonly RelationalScaffoldingModelFactory proxy;
    private readonly MethodInfo visitForeignKeyMethod;
    private readonly MethodInfo addNavigationPropertiesMethod;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledRelationalScaffoldingModelFactory(IServiceProvider serviceProvider,
        IOperationReporter reporter,
        IDesignTimeRuleLoader designTimeRuleLoader) {
        this.reporter = reporter;
        this.designTimeRuleLoader = designTimeRuleLoader;

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
        primitiveNamingRules ??= designTimeRuleLoader?.GetPrimitiveNamingRules() ?? new();


        if (!TryResolveRuleFor(column, out _, out var tableRule, out var columnRule)) return typeScaffoldingInfo;
        if (columnRule?.NewType.HasNonWhiteSpace() != true) return typeScaffoldingInfo;

        try {
            var clrTypeName = columnRule.NewType;
            var clrType = designTimeRuleLoader?.TryResolveType(clrTypeName, typeScaffoldingInfo?.ClrType);

            if (clrType == null) {
                reporter?.WriteWarning($"Type not found: {columnRule.NewType}");
                return typeScaffoldingInfo;
            }

            // Regenerate the TypeScaffoldingInfo based on our new CLR type.
            typeScaffoldingInfo = typeScaffoldingInfo.WithType(clrType);
            WriteVerbose($"Column rule applied: {tableRule.Name}.{columnRule.PropertyName} type set to {columnRule.NewType}");
            return typeScaffoldingInfo;
        } catch (Exception ex) {
            reporter?.WriteWarning($"Error loading type '{columnRule.NewType}' reference: {ex.Message}");
        }

        return typeScaffoldingInfo;
    }


    /// <summary> Get the type changing rule for this column </summary>
    protected virtual bool TryResolveRuleFor(DatabaseColumn column, out SchemaRule schemaRule, out TableRule tableRule,
        out ColumnRule columnRule) {
        return primitiveNamingRules.TryResolveRuleFor(column?.Table?.Schema, column?.Table?.Name, column?.Name,
            out schemaRule, out tableRule, out columnRule);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual EntityTypeBuilder VisitTable(ModelBuilder modelBuilder, DatabaseTable table, Func<EntityTypeBuilder> baseCall) {
        // ReSharper disable once AssignNullToNotNullAttribute
        if (table is null) return baseCall();

        primitiveNamingRules ??= designTimeRuleLoader?.GetPrimitiveNamingRules() ?? new PrimitiveNamingRules();

        if (!primitiveNamingRules.TryResolveRuleFor(table.Schema, table.Name, out _, out var tableRule))
            return baseCall();

        var excludedColumns = new List<DatabaseColumn>();
        if (tableRule?.Columns?.Count > 0)
            foreach (var column in tableRule.Columns.Where(o => o.NotMapped)) {
                var columnToRemove = table.Columns.FirstOrDefault(c => c.Name.Equals(column.Name, StringComparison.OrdinalIgnoreCase));
                if (columnToRemove == null) continue;
                excludedColumns.Add(columnToRemove);
                table.Columns.Remove(columnToRemove);
            }

        if (excludedColumns.Count == 0) return baseCall();

        var indexesToBeRemoved = new List<DatabaseIndex>();
        foreach (var index in table.Indexes)
        foreach (var column in index.Columns)
            if (excludedColumns.Contains(column))
                indexesToBeRemoved.Add(index);

        foreach (var index in indexesToBeRemoved) table.Indexes.Remove(index);

        return baseCall();
    }


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual ModelBuilder VisitForeignKeys(ModelBuilder modelBuilder, IList<DatabaseForeignKey> foreignKeys,
        Func<IList<DatabaseForeignKey>, ModelBuilder> baseCall) {
        if (visitForeignKeyMethod == null || addNavigationPropertiesMethod == null) return baseCall(foreignKeys);

        ArgumentNullException.ThrowIfNull(foreignKeys);
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var schemaNames = foreignKeys.Select(o => o.Table?.Schema).Where(o => o.HasNonWhiteSpace()).Distinct().ToArray();

        var schemas = schemaNames.Select(o => primitiveNamingRules?.TryResolveRuleFor(o))
            .Where(o => o?.UseManyToManyEntity == true).ToArray();

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
            reporter?.WriteInformation($"{schema} Simple ManyToMany junctions are being forced to generate entities for schema {schema}");
            foreach (var fk in schemaForeignKeys)
                visitForeignKeyMethod!.Invoke(proxy, new object[] { modelBuilder, fk });

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            foreach (var foreignKey in entityType.GetForeignKeys())
                addNavigationPropertiesMethod!.Invoke(proxy, new object[] { foreignKey });
        }

        return modelBuilder;
    }


    internal void WriteVerbose(string msg) {
        reporter?.WriteVerbose(msg);
        DesignTimeRuleLoader.DebugLog(msg);
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