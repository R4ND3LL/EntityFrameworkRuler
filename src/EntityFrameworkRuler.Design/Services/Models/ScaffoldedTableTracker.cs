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
    private readonly Dictionary<string, DatabaseTableNode> omittedTables = new();

    /// <summary> An omitted schema implies the mapping of any object within that schema is forbidden. </summary>
    private readonly HashSet<string> omittedSchemas = new();

    /// <summary> An omitted foreign key implies the mapping of any navigation based on this FK is forbidden. </summary>
    private readonly HashSet<IMutableForeignKey> omittedForeignKeys = new();

    private Dictionary<string, Dictionary<string, DatabaseTableNode>> tablesBySchema;
    private DbContextRuleNode dbContextRule;

    /// <summary> Enumerate schemas and tables </summary>
    public IEnumerable<(string Schema, IEnumerable<DatabaseTableNode> Tables)> Tables =>
        tablesBySchema.Select(o => (o.Key, o.Value.Values.Select(n => n)));

    /// <summary> True if there are schema or table omissions so far </summary>
    public bool HasOmissions => omittedSchemas.Count > 0 || omittedTables.Count > 0;

    /// <summary> Initialize data for tracking </summary>
    public void InitializeScope(IEnumerable<DatabaseTable> tables, DbContextRuleNode dbContextRuleNode) {
        tablesBySchema = tables.GroupBy(o => o.Schema.EmptyIfNullOrWhitespace())
            .ToDictionary(o => o.Key, o => o.ToDictionary(t => t.Name, t => new DatabaseTableNode(this, t)));
        dbContextRule = dbContextRuleNode ?? throw new ArgumentNullException(nameof(dbContextRuleNode));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IEnumerable<IMutableForeignKey> GetOmittedForeignKeys() => omittedForeignKeys;

    /// <summary> Omit this item </summary>
    public void OmitSchema(string schema) {
        schema = schema.EmptyIfNullOrWhitespace();
        if (omittedSchemas.Add(schema))
            OnSchemaOmitted(schema);
    }


    /// <summary> Omit this item </summary>
    public void OmitTable(DatabaseTableNode table) => OmitTable(table.Table);

    /// <summary> Omit this item </summary>
    public void OmitTable(DatabaseTable table) {
        var omitted = false;
        omittedTables.GetOrAddNew(table.GetFullName(), AddFactory);

        DatabaseTableNode AddFactory(string name) {
            omitted = true;
            return FindTableNode(table) ?? throw new("Table is null: " + name);
        }

        if (omitted) OnTableOmitted(table);
    }

    private void OnSchemaOmitted(string schema) {
        reporter.WriteInformation($"RULED: Schema {schema} omitted.");
    }

    private void OnTableOmitted(DatabaseTable table) {
        if (!IsSchemaOmitted(table))
            reporter.WriteInformation($"RULED: {(table is DatabaseView ? "View" : "Table")}  {table.GetFullName()} omitted.");
    }


    /// <summary> Omit this item </summary>
    public void Omit(IMutableForeignKey foreignKey) {
        this.omittedForeignKeys.Add(foreignKey);
    }


    /// <summary> Get the table nodes by schema name </summary>
    private Dictionary<string, DatabaseTableNode> FindSchemaTables(string schemaName) {
        return tablesBySchema.TryGetValue(schemaName.EmptyIfNullOrWhitespace());
    }

    /// <summary> Get the table node </summary>
    public DatabaseTableNode FindTableNode(string schemaName, string tableName) {
        return tablesBySchema.TryGetValue(schemaName.EmptyIfNullOrWhitespace())?.TryGetValue(tableName);
    }

    /// <summary> Get the table node </summary>
    public DatabaseTableNode FindTableNode(DatabaseTable table) {
        var node = tablesBySchema.TryGetValue(table.Schema.EmptyIfNullOrWhitespace())?.TryGetValue(table.Name);
        Debug.Assert(node != null, "This tracker should have been initialized with the entire table set");
        return node;
    }

    /// <summary> true if omitted </summary>
    public bool IsSchemaOmitted(DatabaseTable table) => IsSchemaOmitted(table.Schema);

    /// <summary> true if omitted </summary>
    public bool IsSchemaOmitted(string schema) => omittedSchemas.Contains(schema.EmptyIfNullOrWhitespace());

    /// <summary> true if omitted </summary>
    public bool IsOmitted(DatabaseTable table) {
        if (table?.Name == null) return false;
        if (IsSchemaOmitted(table)) return true;
        if (omittedTables.ContainsKey(table.GetFullName())) return true;
        return false;
    }

    /// <summary> true if omitted </summary>
    public bool IsOmitted(DatabaseForeignKey fk) {
        if (fk?.Name == null) return false;
        if (IsOmitted(fk.Table) || IsOmitted(fk.PrincipalTable)) return true;
        return IsOmittedCols(fk.Columns) || IsOmittedCols(fk.PrincipalColumns);

        bool IsOmittedCols(IList<DatabaseColumn> columns) {
            if (columns == null || columns.Count == 0) return false;
            var entityRules = dbContextRule.TryResolveRuleFor(columns[0].Table);
            Debug.Assert(entityRules?.Any(o => o.Rule.ShouldMap()) == true, "Rule should exist since table/schema has not been omitted");
            if (entityRules.Count == 0) return false;
            foreach (var column in columns) {
                var propertyRuleNodes = entityRules
                    .Select(o => o.TryResolveRuleFor(column.Name))
                    .Where(o => o != null).ToArray();

                if (propertyRuleNodes.Length == 0 &&
                    entityRules.All(o => !o.Rule.ShouldMap() || !o.Rule.IncludeUnknownColumns)) return true;
                if (propertyRuleNodes.Length == 0) continue; // implicit inclusion
                if (propertyRuleNodes.Any(o => o.Property != null)) continue; // it has been scaffolded
                if (propertyRuleNodes.All(o => !o.Rule.ShouldMap())) return true; // property omitted on all entities
            }

            return false;
        }
    }
}