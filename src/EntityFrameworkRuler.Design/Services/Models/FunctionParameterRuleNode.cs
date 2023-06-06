using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class FunctionParameterRuleNode : RuleNode<FunctionParameterRule, FunctionRuleNode> {
    /// <inheritdoc />
    public FunctionParameterRuleNode(FunctionParameterRule r, FunctionRuleNode parent) : base(r, parent) { }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DbContextRuleNode DbContextRuleNode => Parent?.DbContextRuleNode;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IParameter Parameter { get; private set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string Name { get; private set; }

    /// <inheritdoc />
    public override string GetFinalName() => Parameter?.Name ?? base.GetFinalName();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public void MapTo(IParameter property, string dbName) {
        Parameter = property;
        Name = dbName;
        UpdateRuleMetadata();
    }

    private void UpdateRuleMetadata() {
        if (Parameter == null || Name == null) return;
        Rule.Name = Name;

       
        //if (Parameter.ClrType?.IsEnum == true) Rule.TypeName = Parameter.ClrType.ToFriendlyTypeName();
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator FunctionParameterRule(FunctionParameterRuleNode o) => o?.Rule;
}