using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class DbContextRuleNode : RuleNode<DbContextRule, DbContextRuleNode> {
    private SortedSet<EntityRuleNode> entities;

    /// <inheritdoc />
    public DbContextRuleNode(DbContextRule r) : base(r, null) {
        r ??= DbContextRule.DefaultNoRulesFoundBehavior;
        Schemas = new(() => r.Schemas.Select(o => new SchemaRuleNode(o, this)));
        ForeignKeys = new(() => r.ForeignKeys.Select(o => new ForeignKeyRuleNode(o, this)));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public SortedSet<EntityRuleNode> Entities => entities ??= InitializeEntitiesOrdered();

    private SortedSet<EntityRuleNode> InitializeEntitiesOrdered() {
        var es = new SortedSet<EntityRuleNode>(EntityDependencyComparer.Instance);
        var added = 0;
        foreach (var e in Schemas.SelectMany(o => o.Entities)) {
            if (e.Rule.BaseTypeName.HasNonWhiteSpace()) ResolveBaseEntity(e);
            es.Add(e);
            added++;
            Debug.Assert(es.Count == added);
        }

        return es;

        void ResolveBaseEntity(EntityRuleNode e) {
            // base entity is resolved by the final entity name (not DB name)
            e.BaseEntityRuleNode = e.Parent.Entities.GetByFinalName(e.Rule.BaseTypeName);
            if (e.BaseEntityRuleNode != null) return;
            foreach (var schema in Schemas.Where(o => o != e.Parent)) {
                e.BaseEntityRuleNode = schema.Entities.GetByFinalName(e.Rule.BaseTypeName);
                if (e.BaseEntityRuleNode != null) return;
            }

            if (e.BaseEntityRuleNode == null) {
                // Couldn't resolve the base type!  This will be not generate the expected output
            }
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleIndex<SchemaRuleNode> Schemas { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleIndex<ForeignKeyRuleNode> ForeignKeys { get; }

    private readonly Dictionary<string, EntityRuleNode> entityRulesByFinalName = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal void Map(EntityTypeBuilder entityTypeBuilder, EntityRuleNode entityRule) {
        if (entityTypeBuilder == null) throw new ArgumentNullException(nameof(entityTypeBuilder));
        entityRulesByFinalName.Add(entityTypeBuilder.Metadata.Name, entityRule);
        Debug.Assert(entityRule?.DatabaseTable != null && entityRule.DatabaseTable.EntityRules.Contains(entityRule));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public EntityRuleNode TryResolveRuleForEntityName(string entityName) {
        if (entityName.IsNullOrWhiteSpace()) return null;
        return entityRulesByFinalName.TryGetValue(entityName);
    }


    /// <summary> Get the schema rule for the given target schema. Used during scaffolding phase. </summary>
    public SchemaRuleNode TryResolveRuleFor(string schema) {
        if (Schemas.Count == 0) return null;
        if (schema.IsNullOrWhiteSpace()) schema = string.Empty;

        var schemaRule = Schemas.GetByDbName(schema);

        if (schemaRule == null && schema.IsNullOrWhiteSpace()) {
            // default to dbo, otherwise fail
            if (Schemas.Count == 1 && (Schemas[0].Rule.SchemaName.IsNullOrWhiteSpace() || Schemas[0].Rule.SchemaName == "dbo"))
                return Schemas[0];
            return null;
        }

        // if there is only one schema, and the name is not defined, default to that
        if (schemaRule == null && Schemas.Count == 1 && Schemas[0].Rule.SchemaName.IsNullOrWhiteSpace())
            schemaRule = Schemas[0];

        return schemaRule;
    }

    /// <summary> Get the table rule for the given target table. Used during scaffolding phase. </summary>
    public ICollection<EntityRuleNode> TryResolveRuleFor(DatabaseTable table) =>
        TryResolveRuleFor(table.Schema, table.Name);

    /// <summary> Get the table rule for the given target table. Used during scaffolding phase. </summary>
    public ICollection<EntityRuleNode> TryResolveRuleFor(string schema, string table) =>
        TryResolveRuleFor(schema)?.TryResolveRuleFor(table) ?? Array.Empty<EntityRuleNode>();

    /// <summary> Return true if the given scheme can be mapped. </summary>
    public bool ShouldMapSchema(string schema) {
        var x = Schemas.GetByDbName(schema);
        if (x?.NotMapped == true) return false;
        return Rule.IncludeUnknownSchemas || x != null;
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator DbContextRule(DbContextRuleNode o) => o?.Rule;


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public SchemaRuleNode AddSchema(string schema) {
        var schemaRule = new SchemaRuleNode(new SchemaRule {
            SchemaName = schema.EmptyIfNullOrWhitespace(),
            IncludeUnknownTables = true,
            IncludeUnknownViews = true,
        }, this);
        Schemas.Add(schemaRule);
        Rule.Schemas.Add(schemaRule);
        return schemaRule;
    }
}