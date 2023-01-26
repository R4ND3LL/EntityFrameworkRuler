using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <summary> Links both ends of a navigation relationship according to the constraint name. </summary>
[DebuggerDisplay("{FkName}")]
public sealed class ForeignKeyRuleNode : RuleNode<ForeignKeyRule, DbContextRuleNode> {
    /// <inheritdoc />
    public ForeignKeyRuleNode(ForeignKeyRule r, DbContextRuleNode parent) : base(r, parent) {
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string FkName => Rule.Name;

    /// <summary> True if rule fk definition is valid </summary>
    public bool IsRuleValid => Rule != null && !Rule.DependentProperties.IsNullOrEmpty() &&
                               Rule.DependentProperties.Length == Rule.PrincipalProperties.Length &&
                               Rule.DependentEntity.HasNonWhiteSpace() && Rule.PrincipalEntity.HasNonWhiteSpace() &&
                               Rule.DependentProperties.All(o => o.HasNonWhiteSpace()) &&
                               Rule.PrincipalProperties.All(o => o.HasNonWhiteSpace());

    /// <summary> The scaffolded database foreign key </summary>
    public DatabaseForeignKey ForeignKey { get; private set; }

    /// <summary> Indicate this rule is linked to the given FK </summary>
    public void MapTo(DatabaseForeignKey foreignKey) {
        Debug.Assert(ForeignKey == null);
        ForeignKey = foreignKey;
    }
}