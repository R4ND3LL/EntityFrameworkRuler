using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class ScaffoldedTableTrackerItem {
    private readonly ScaffoldedTableTracker tracker;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ScaffoldedTableTrackerItem(ScaffoldedTableTracker tracker, DatabaseTable table) {
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

    public bool IsFakeTable => Table is FakeDatabaseTable;
    public FakeDatabaseTable AsFakeTable => Table as FakeDatabaseTable;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string FullName => Table.GetFullName();

    /// <summary> Associate this table to the entity rule, and thus the scaffolded entity type </summary>
    public void MapTo(EntityRuleNode entityRule) {
        Debug.Assert(!IsFakeTable || Table is TphDatabaseTable);
        EntityRules.Add(entityRule);
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator DatabaseTable(ScaffoldedTableTrackerItem o) => o?.Table;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IMutableEntityType FunctionEntityType { get; private set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public void MapFunctionTo(IMutableEntityType entityType) {
        FunctionEntityType = entityType;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Schema}.{Name}";
}