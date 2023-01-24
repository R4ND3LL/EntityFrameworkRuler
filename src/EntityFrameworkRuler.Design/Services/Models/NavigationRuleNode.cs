using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class NavigationRuleNode : RuleNode<NavigationRule, EntityRuleNode> {
    /// <inheritdoc />
    public NavigationRuleNode(NavigationRule r, EntityRuleNode parent) : base(r, parent) { }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DbContextRuleNode DbContextRuleNode => Parent?.DbContextRuleNode;

    /// <summary> The foreign key name for this relationship (if any). </summary>
    public string FkName => Rule.FkName;

    /// <summary> True if, this is the principal end of the navigation.  False if this is the dependent end. </summary>
    public bool IsPrincipal => Rule.IsPrincipal;

    /// <summary> If false, omit this column during the scaffolding process. </summary>
    public bool ShouldMap => Rule.ShouldMap();

    /// <summary> True if this navigation has been mapped to an entity builder </summary>
    public bool IsAlreadyMapped => Navigation != null;

    /// <summary> True if this navigation has not been mapped yet but can be </summary>
    public bool IsPendingMapping => ShouldMap && !IsAlreadyMapped && Parent.IsAlreadyMapped && Rule.ToEntity.HasNonWhiteSpace() &&
                                    (Rule.Name.HasNonWhiteSpace() || Rule.NewName.HasNonWhiteSpace());

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IMutableNavigation Navigation { get; private set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public void MapTo(IMutableNavigation navigation, string fkName, bool thisIsPrincipal, bool isManyToMany) {
        Navigation = navigation;

        Rule.FkName = fkName;
        Rule.IsPrincipal = thisIsPrincipal || isManyToMany;
        if (!string.IsNullOrWhiteSpace(navigation.TargetEntityType.Name))
            Rule.ToEntity = navigation.TargetEntityType.Name;

        var m = navigation.GetMultiplicity().ToMultiplicityString();
        if (Rule.Multiplicity != m) Rule.Multiplicity = m;
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator NavigationRule(NavigationRuleNode o) => o?.Rule;
}