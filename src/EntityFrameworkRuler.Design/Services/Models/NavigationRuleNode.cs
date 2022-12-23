using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class NavigationRuleNode : RuleNode<NavigationRule, EntityRuleNode> {
    /// <inheritdoc />
    public NavigationRuleNode(NavigationRule r, EntityRuleNode parent) : base(r, parent) { }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string FkName => Rule.FkName;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IMutableNavigation Navigation { get; private set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public void MapTo(IMutableNavigation navigation, string fkName, bool thisIsPrincipal, bool isManyToMany) {
        Navigation = navigation;

        Rule.FkName = fkName;
        Rule.IsPrincipal = thisIsPrincipal || isManyToMany;
        if (!string.IsNullOrWhiteSpace(navigation.TargetEntityType.Name))
            Rule.ToEntity = navigation.TargetEntityType.Name;
        Rule.Multiplicity = navigation.GetMultiplicity().ToMultiplicityString();
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator NavigationRule(NavigationRuleNode o) => o?.Rule;
}