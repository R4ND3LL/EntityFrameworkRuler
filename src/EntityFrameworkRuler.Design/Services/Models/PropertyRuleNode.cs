using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class PropertyRuleNode : RuleNode<PropertyRule, EntityRuleNode> {
    /// <inheritdoc />
    public PropertyRuleNode(PropertyRule r, EntityRuleNode parent) : base(r, parent) { }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DbContextRuleNode DbContextRuleNode => Parent?.DbContextRuleNode;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IMutableProperty Property { get; private set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string ColumnName { get; private set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public void MapTo(IMutableProperty property, string column) {
        Property = property;
        ColumnName = column;
        UpdateRuleMetadata();
    }

    private void UpdateRuleMetadata() {
        if (Property == null || ColumnName == null) return;
        Rule.Name = ColumnName;

        // can only update expect name if it wasn't already influenced by dynamic naming or NewName
        if (Rule.NewName.IsNullOrWhiteSpace() && !Parent.Parent.IsDynamicNamingColumns &&
            (Rule.PropertyName.HasNonWhiteSpace() || Property.Name != ColumnName))
            Rule.PropertyName = Property.Name;

        if (Property.ClrType?.IsEnum == true) Rule.NewType = Property.ClrType.ToFriendlyTypeName();
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator PropertyRule(PropertyRuleNode o) => o?.Rule;
}