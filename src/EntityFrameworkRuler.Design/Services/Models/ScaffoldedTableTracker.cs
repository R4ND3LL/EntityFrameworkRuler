using System.Collections;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Extensions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <summary> Object that tracks objects that have been scaffolded or omitted. </summary>
public sealed class ScaffoldedTableTracker {
    private readonly IMessageLogger reporter;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ScaffoldedTableTracker(IMessageLogger reporter) {
        this.reporter = reporter;
    }

    /// <summary> An omitted schema implies the mapping of any entity to this table is forbidden. </summary>
    private readonly HashSet<EntityRuleNode> omittedEntities = new();

    /// <summary> An omitted schema implies the mapping of any object within that schema is forbidden. </summary>
    private readonly HashSet<string> omittedSchemas = new();

    /// <summary> An omitted foreign key implies the mapping of any navigation based on this FK is forbidden. </summary>
    private readonly Dictionary<IMutableForeignKey, ForeignKeyUsage> foreignKeyUsage = new();

    private Dictionary<string, Dictionary<string, ScaffoldedTableTrackerItem>> tablesBySchema;
    private DbContextRuleNode dbContextRule;

    /// <summary> Enumerate schemas and tables </summary>
    public IEnumerable<(string Schema, IEnumerable<ScaffoldedTableTrackerItem> Tables)> Tables =>
        tablesBySchema.Select(o => (o.Key, o.Value.Values.Select(n => n)));

    /// <summary> True if there are schema or table omissions so far </summary>
    public bool HasOmissions => omittedSchemas.Count > 0 || omittedEntities.Count > 0;

    /// <summary> Initialize data for tracking </summary>
    public void InitializeScope(IEnumerable<DatabaseTable> tables, DbContextRuleNode dbContextRuleNode) {
        tablesBySchema = tables.GroupBy(o => o.Schema.EmptyIfNullOrWhitespace())
            .ToDictionary(o => o.Key, o => o.ToDictionary(t => t.Name, t => new ScaffoldedTableTrackerItem(this, t)));
        dbContextRule = dbContextRuleNode ?? throw new ArgumentNullException(nameof(dbContextRuleNode));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IEnumerable<IMutableForeignKey> GetOmittedForeignKeys() =>
        foreignKeyUsage.Values.Where(o => o.Usage == 0).Select(o => o.ForeignKey);

    /// <summary> Omit this item </summary>
    public void OmitSchema(string schema) {
        schema = schema.EmptyIfNullOrWhitespace();
        if (omittedSchemas.Add(schema))
            OnSchemaOmitted(schema);
    }


    /// <summary> Omit this item </summary>
    public void Omit(EntityRuleNode entityRule) {
        if (entityRule == null) throw new ArgumentNullException(nameof(entityRule));
        var omitted = omittedEntities.Add(entityRule);
        //var entityName = entityRule.GetFinalName();
        // if (omitted && entityName.HasNonWhiteSpace())
        //     omittedEntitiesByFinalName.GetOrAddNew(entityName, AddFactory);
        //
        // EntityRuleNode AddFactory(string entityName2) => entityRule;

        if (omitted) OnEntityOmitted(entityRule);
    }

    /// <summary> Omit this item </summary>
    public void Omit(PropertyRuleNode propertyRule) {
        propertyRule?.SetOmitted();
    }

    /// <summary> Track foreign key usage. Given that FK may be used on multiple entities (split tables), usage has to
    /// be tracked overall such that the FK will removed from the model only if unused by any entity. </summary>
    public void CountForeignKeyUsage(IMutableForeignKey foreignKey, string fkName, bool used) {
        //fkName ??= foreignKey.GetConstraintNameForTableOrView();
        var usage = foreignKeyUsage.GetOrAddNew(foreignKey, AddFactory);
        if (used) {
            usage.Usage++;
            Debug.Assert(usage.Usage > 0);
        }

        static ForeignKeyUsage AddFactory(IMutableForeignKey arg) => new(arg);
    }

    private void OnSchemaOmitted(string schema) {
        reporter.WriteInformation($"RULED: Schema {schema} omitted.");
        var schemaRuleNode = dbContextRule?.TryResolveRuleFor(schema);
        schemaRuleNode?.SetOmitted();
    }

    private void OnEntityOmitted(EntityRuleNode entityRule) {
        if (entityRule == null) return;
        var schemaName = entityRule.ScaffoldedTable?.Schema ?? entityRule.Parent?.Rule?.SchemaName;
        entityRule.SetOmitted();
        if (!IsSchemaOmitted(schemaName))
            reporter.WriteInformation($"RULED: Entity {entityRule.GetFinalName()} omitted.");
    }


    // private void OnForeignKeyOmitted(IMutableForeignKey foreignKey) {
    //     if (foreignKey == null) return;
    //     var name = foreignKey.GetConstraintNameForTableOrView();
    //     var node = name.HasNonWhiteSpace() ? dbContextRule?.ForeignKeys?.GetByDbName(name) : null;
    //     node?.SetOmitted();
    // }

    // /// <summary> Get the table nodes by schema name </summary>
    // private Dictionary<string, ScaffoldedTableTrackerItem> FindSchemaTables(string schemaName) {
    //     return tablesBySchema.TryGetValue(schemaName.EmptyIfNullOrWhitespace());
    // }

    /// <summary> Get the table node </summary>
    public ScaffoldedTableTrackerItem FindTableNode(string schemaName, string tableName) {
        return tablesBySchema.TryGetValue(schemaName.EmptyIfNullOrWhitespace())?.TryGetValue(tableName);
    }

    /// <summary> Get the table node </summary>
    public ScaffoldedTableTrackerItem FindTableNode(DatabaseTable table) {
        if (table == null) return null;
        var node = tablesBySchema.TryGetValue(table.Schema.EmptyIfNullOrWhitespace())?.TryGetValue(table.Name);
        Debug.Assert(node != null, "This tracker should have been initialized with the entire table set");
        return node;
    }

    /// <summary> true if omitted </summary>
    private bool IsSchemaOmitted(DatabaseTable table) => IsSchemaOmitted(table.Schema);

    /// <summary> true if omitted </summary>
    private bool IsSchemaOmitted(string schema) => omittedSchemas.Contains(schema.EmptyIfNullOrWhitespace());

    /// <summary> true if omitted </summary>
    private bool IsOmitted(DatabaseTable table) {
        if (table?.Name == null) return false;
        if (IsSchemaOmitted(table)) return true;
        var node = FindTableNode(table);
        Debug.Assert(node != null);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        return node == null || node.EntityRules.Count == 0 || node.EntityRules.All(o => !o.ShouldMap());
    }

    /// <summary> true if omitted.  To be evaluated only AFTER all entities and properties have been scaffolded. </summary>
    public bool IsOmitted(DatabaseForeignKey fk) {
        if (fk?.Name == null) return false;
        if (IsOmitted(fk.Table) || IsOmitted(fk.PrincipalTable)) return true;
        return IsOmittedCols(fk.Columns) || IsOmittedCols(fk.PrincipalColumns);

        bool IsOmittedCols(IList<DatabaseColumn> columns) {
            if (columns == null || columns.Count == 0) return false;
            var entityRules = dbContextRule.TryResolveRuleFor(columns[0].Table);
            Debug.Assert(entityRules.Any(o => o.ShouldMap()) == true, "Rule should exist since table/schema has not been omitted");
            if (entityRules.Count == 0) return true; // rules are missing, has to have been omitted. 
            foreach (var column in columns) {
                var propertyRuleNodes = entityRules
                    .Select(o => o.TryResolveRuleFor(column.Name))
                    .Where(o => o != null).ToArray();

                if (propertyRuleNodes.Length == 0 &&
                    entityRules.All(o => !o.ShouldMap() || !o.Rule.IncludeUnknownColumns)) return true;
                if (propertyRuleNodes.Length == 0) continue; // implicit inclusion
                if (propertyRuleNodes.All(o => !o.ShouldMap() || o.Property == null)) return true; // property omitted on all entities
            }

            return false;
        }
    }

    private sealed class ForeignKeyUsage {
        public ForeignKeyUsage(IMutableForeignKey foreignKey) {
            ForeignKey = foreignKey;
        }

        public IMutableForeignKey ForeignKey { get; }
        public int Usage { get; set; }
    }
}