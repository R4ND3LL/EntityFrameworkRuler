using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class EntityRuleNode : RuleNode<EntityRule, SchemaRuleNode> {
    /// <inheritdoc />
    public EntityRuleNode(EntityRule r, SchemaRuleNode parent) : base(r, parent) {
        Properties = new(() => r.Properties.Select(o => new PropertyRuleNode(o, this)));
        Navigations = new(() => r.Navigations.Select(o => new NavigationRuleNode(o, this)));
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DbContextRuleNode DbContextRuleNode => Parent?.Parent;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public EntityRuleNode BaseEntityRuleNode { get; set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleIndex<PropertyRuleNode> Properties { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleIndex<NavigationRuleNode> Navigations { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DatabaseTableNode DatabaseTable { get; private set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public EntityTypeBuilder Builder { get; private set; }

    /// <summary> True if this entity has been mapped to an entity builder </summary>
    public bool IsAlreadyMapped => Builder != null && DatabaseTable != null;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public void MapTo(EntityTypeBuilder builder) {
        Debug.Assert(Builder == null);
        Builder = builder;
        Parent.Parent.Map(builder, this);
        UpdateRuleMetadata();
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public void MapTo(DatabaseTableNode table) {
        Debug.Assert(DatabaseTable == null);
        DatabaseTable = table;
        Parent.Parent.Map(table, this);
        UpdateRuleMetadata();
    }

    private void UpdateRuleMetadata() {
        if (DatabaseTable == null) return;
        Rule.Name = DatabaseTable.Name;
        if (Builder == null) return;

        // can only update expected name if it wasn't already influenced by dynamic naming or NewName
        if (Rule.NewName.IsNullOrWhiteSpace() && !Parent.IsDynamicNamingTables &&
            (Rule.EntityName.HasNonWhiteSpace() || Builder.Metadata.Name != (DatabaseTable?.Name ?? Rule.Name)))
            Rule.EntityName = Builder.Metadata.Name;
    }

    /// <summary> get the inner most base type, or this if no base type exists </summary>
    public EntityRuleNode GetHierarchyRoot() {
        var b = BaseEntityRuleNode;
        while (b?.BaseEntityRuleNode != null) b = b.BaseEntityRuleNode;
        return b ?? this;
    }

    /// <summary> get all the base types </summary>
    public IEnumerable<EntityRuleNode> GetBaseTypes(bool startWithThis = false) {
        if (startWithThis) yield return this;
        var b = BaseEntityRuleNode;
        while (b != null) {
            yield return b;
            b = b.BaseEntityRuleNode;
        }
    }

    /// <summary> Return true if the given node is among the base types </summary>
    public bool HasBaseType(EntityRuleNode node) {
        var b = BaseEntityRuleNode;
        while (b != null) {
            if (b == node) return true;
            b = b.BaseEntityRuleNode;
        }

        return false;
    }

    /// <summary> Return true if the given rule is among the base types </summary>
    public bool HasBaseType(EntityRule node) {
        var b = BaseEntityRuleNode;
        while (b != null) {
            if (b.Rule == node) return true;
            b = b.BaseEntityRuleNode;
        }

        return false;
    }

    /// <summary> Get the property rule for the given target column. Used during scaffolding phase. </summary>
    public PropertyRuleNode TryResolveRuleFor(string column) {
        if (Properties == null || Properties.Count == 0 || column.IsNullOrWhiteSpace()) return null;

        var entityRule = Properties.GetByDbName(column);
        if (entityRule != null) return entityRule;

        entityRule = Properties.GetByFinalName(column);
        if (entityRule?.DbName.HasNonWhiteSpace() == true) entityRule = null;

        return entityRule;
    }

    /// <summary> Return the navigation naming rule for the given navigation info </summary>
    public NavigationRuleNode TryResolveNavigationRuleFor(string fkName, Func<string> defaultEfName, bool thisIsPrincipal,
        bool isManyToMany) {
        if (Navigations == null || Navigations.Count == 0) return null;

        // locate by fkName first, which is most reliable.
        var navigations = fkName.HasNonWhiteSpace()
            ? Navigations
                .Where(t => t.FkName == fkName)
                .ToArray()
            : Array.Empty<NavigationRuleNode>();

        if (navigations.Length == 0) {
            // Maybe FinalName is not defined?  if not, try to locate by expected target name instead
            if (fkName.HasNonWhiteSpace()) {
                var someFinalNamesEmpty = Navigations.Any(o => o.FkName.IsNullOrWhiteSpace());
                if (!someFinalNamesEmpty)
                    // Final names ARE defined, this property is just not found. Use default.
                    return null;
            }

            var efName = defaultEfName();
            navigations = Navigations.Where(o => o.Rule.Name.HasNonWhiteSpace() && o.Rule.Name.EqualsIgnoreCase(efName))
                .ToArray();
            if (navigations.Length == 0) return null; // expected EF name resolution failed to
        }

        // we have candidate matches (by fk name or expected target name). we may need to narrow further.
        // ReSharper disable once InvertIf
        if (navigations.Length > 1) {
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (isManyToMany)
                // many-to-many relationships always set IsPrincipal=true for both ends in the rule file.
                navigations = navigations.Where(o => o.Rule.IsPrincipal).ToArray();
            else
                // filter for this end only
                navigations = navigations.Where(o => o.Rule.IsPrincipal == thisIsPrincipal).ToArray();
        }

        return navigations.Length != 1 ? null : navigations[0];
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public PropertyRuleNode AddProperty(IMutableProperty property, string column) {
        var rule = new PropertyRuleNode(new PropertyRule {
            Name = column,
            PropertyName = property.Name
        }, this);
        Properties.Add(rule);
        Rule.Properties.Add(rule);
        return rule;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public NavigationRuleNode AddNavigation(IMutableNavigation navigation, string fkName, bool thisIsPrincipal, bool isManyToMany) {
        var rule = new NavigationRuleNode(new NavigationRule {
            Name = navigation.Name,
            FkName = fkName,
            IsPrincipal = thisIsPrincipal || isManyToMany
        }, this);
        Navigations.Add(rule);
        Rule.Navigations.Add(rule);
        return rule;
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator EntityRule(EntityRuleNode o) => o?.Rule;
}