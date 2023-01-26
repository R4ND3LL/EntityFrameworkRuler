using EntityFrameworkRuler.Design.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class DatabaseTableNode {
    private readonly ScaffoldedTableTracker tracker;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DatabaseTableNode(ScaffoldedTableTracker tracker, DatabaseTable table) {
        this.tracker = tracker;
        Table = table;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DatabaseTable Table { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public HashSet<EntityRuleNode> EntityRules { get; } = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IEnumerable<EntityTypeBuilder> Builders => EntityRules.Select(o => o.Builder).Where(o => o != null);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string Schema => Table.Schema.EmptyIfNullOrWhitespace();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string Name => Table.Name;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string FullName => Table.GetFullName();

    /// <summary> True if this table is forbidden to be mapped to an entity </summary>
    public bool GetIsOmitted() => tracker.IsOmitted(this);

    /// <summary> Associate this table to the entity rule, and thus the scaffolded entity type </summary>
    public void MapTo(EntityRuleNode entityRule) {
        if (GetIsOmitted()) {
            // check to ensure we are not scaffolding this entity
            if (entityRule.Builder != null || entityRule.Rule.ShouldMap())
                throw new($"Entity {entityRule.GetFinalName() ?? Table.Name} is being scaffolded, but was previously omitted");
        }

        EntityRules.Add(entityRule);
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator DatabaseTable(DatabaseTableNode o) => o?.Table;
}