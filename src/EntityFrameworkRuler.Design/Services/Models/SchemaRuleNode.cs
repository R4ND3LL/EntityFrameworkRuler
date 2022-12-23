using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class SchemaRuleNode : RuleNode<SchemaRule, DbContextRuleNode> {
    /// <inheritdoc />
    public SchemaRuleNode(SchemaRule r, DbContextRuleNode parent) : base(r, parent) {
        Entities = new(() => r.Entities.Select(o => new EntityRuleNode(o, this)));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleIndex<EntityRuleNode> Entities { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public bool IsDynamicNamingTables =>
        Parent.Rule.PreserveCasingUsingRegex ||
        Rule.UseSchemaName ||
        (Rule.TableRegexPattern.HasNonWhiteSpace() && Rule.TablePatternReplaceWith != null);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public bool IsDynamicNamingColumns =>
        Parent.Rule.PreserveCasingUsingRegex ||
        Rule.UseSchemaName ||
        (Rule.ColumnRegexPattern.HasNonWhiteSpace() && Rule.ColumnPatternReplaceWith != null);

    /// <summary> Get the table rule for the given target table. Used during scaffolding phase. </summary>
    public EntityRuleNode TryResolveRuleFor(string table) {
        if (Entities == null || Entities.Count == 0 || table.IsNullOrWhiteSpace()) return null;

        var entityRule = Entities?.GetByDbName(table);
        if (entityRule != null) return entityRule;

        entityRule = Entities?.GetByFinalName(table);
        if (entityRule?.DbName.HasNonWhiteSpace() == true) entityRule = null;

        return entityRule;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public EntityRuleNode AddEntity(string tableName) {
        var entityRule = new EntityRuleNode(new EntityRule {
            Name = tableName,
            IncludeUnknownColumns = true
        }, this);
        Entities.Add(entityRule);
        Rule.Entities.Add(entityRule);
        return entityRule;
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator SchemaRule(SchemaRuleNode o) => o?.Rule;
}