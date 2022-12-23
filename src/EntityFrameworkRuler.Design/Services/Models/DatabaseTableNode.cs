using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class DatabaseTableNode {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DatabaseTableNode(DatabaseTable table) {
        Table = table;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DatabaseTable Table { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public HashSet<EntityRuleNode> EntityRules { get; } = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public HashSet<EntityTypeBuilder> Builders { get; } = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string Schema => Table.Schema;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string Name => Table.Name;

    /// <summary> implicit conversion </summary>
    public static implicit operator DatabaseTable(DatabaseTableNode o) => o?.Table;
}