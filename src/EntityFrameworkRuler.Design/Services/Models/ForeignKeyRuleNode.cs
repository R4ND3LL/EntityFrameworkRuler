using EntityFrameworkRuler.Rules;

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
                               Rule.DependentEntity.HasNonWhiteSpace() && Rule.PrincipalEntity.HasNonWhiteSpace();

}