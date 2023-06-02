using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class EntityRuleNode : RuleNode<EntityRule, SchemaRuleNode> {
    /// <inheritdoc />
    public EntityRuleNode(EntityRule r, SchemaRuleNode parent) : base(r, parent) {
        Properties = new(() => r.Properties.Select(o => new PropertyRuleNode(o, this)), parent.Parent.Rule.CaseSensitive);
        Navigations = new(() => r.Navigations.Select(o => new NavigationRuleNode(o, this)), parent.Parent.Rule.CaseSensitive);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DbContextRuleNode DbContextRuleNode => Parent?.Parent;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public EntityRuleNode BaseEntityRuleNode { get; set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    private RuleIndex<PropertyRuleNode> Properties { get; }

    /// <summary> Get properties recursively </summary>
    public IEnumerable<PropertyRuleNode> GetProperties()
        => BaseEntityRuleNode != null
            ? BaseEntityRuleNode.GetProperties().Concat(Properties)
            : Properties;

    /// <summary> sum of local properties and navigations </summary>
    public int LocalPropertyCount => Properties.Count + Navigations.Count;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    private RuleIndex<NavigationRuleNode> Navigations { get; }

    /// <summary> Get navigations recursively </summary>
    public IEnumerable<NavigationRuleNode> GetNavigations()
        => BaseEntityRuleNode != null
            ? BaseEntityRuleNode.GetNavigations().Concat(Navigations)
            : Navigations;


    /// <summary> The underlying table for this entity.  May or may not have been omitted yet. </summary>
    public DatabaseTableNode DatabaseTable { get; private set; }

    /// <summary> The entity type builder used to scaffold this table.  Presence of this value implies the entity was not omitted. </summary>
    public EntityTypeBuilder Builder { get; private set; }

    /// <summary> True if this entity has been scaffolded with an entity builder.
    /// Note, will be false if table was identified but entity omitted. </summary>
    public bool IsAlreadyScaffolded => !IsOmitted && Builder != null && DatabaseTable != null;

    /// <summary> True if this entity has been mapped to a database table (or view).  May or may not have been omitted. </summary>
    public bool IsMappedToTable => DatabaseTable != null;

    /// <inheritdoc />
    public override string GetFinalName() => Builder?.Metadata.Name ?? base.GetFinalName();

    /// <summary> Link this entity rule to the scaffolded entity type builder. </summary>
    public void MapTo(EntityTypeBuilder builder, DatabaseTableNode databaseTable) {
        Debug.Assert(Builder == null, "Builder was previously set");
        Debug.Assert(Rule.ShouldMap(), "Entity should not be scaffolded");
        if (DatabaseTable == null) {
            if (databaseTable != null) MapTo(databaseTable);
            if (DatabaseTable == null) throw new("Cannot set entity builder without DatabaseTable");
        } else {
            if (databaseTable != null && !ReferenceEquals(DatabaseTable, databaseTable))
                throw new("DatabaseTable instance mismatch");
        }

        Builder = builder;
        Parent.Parent.Map(builder, this);
        UpdateRuleMetadata();
    }

    /// <summary> Link this entity rule to the underlying database table.  This will be linked whether the entity is omitted or not. </summary>
    public void MapTo(DatabaseTableNode table) {
        Debug.Assert(DatabaseTable == null);
        DatabaseTable = table;
        table.MapTo(this);
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
        if (column.IsNullOrWhiteSpace()) return null;

        var entityRule = FindPropertyByColumn(column);
        if (entityRule != null) return entityRule;

        entityRule = FindPropertyByFinalName(column);
        if (entityRule?.DbName.HasNonWhiteSpace() == true) entityRule = null;

        return entityRule;
    }

    /// <summary> Get the property rule recursively for the given target column. Used during scaffolding phase. </summary>
    public PropertyRuleNode FindPropertyByColumn(string column) {
        if (column.IsNullOrWhiteSpace()) return null;
        var entityRule = Properties.GetByDbName(column) ?? BaseEntityRuleNode?.FindPropertyByColumn(column);
        return entityRule;
    }

    /// <summary> Get the property rule recursively for the given target property name. Used during scaffolding phase. </summary>
    public PropertyRuleNode FindPropertyByFinalName(string propertyName) {
        if (propertyName.IsNullOrWhiteSpace()) return null;
        var entityRule = Properties.GetByFinalName(propertyName) ?? BaseEntityRuleNode?.FindPropertyByFinalName(propertyName);
        return entityRule;
    }

    /// <summary> Get the navigation rule recursively for the given target property name. Used during scaffolding phase. </summary>
    public NavigationRuleNode FindNavigationByFinalName(string propertyName) {
        if (propertyName.IsNullOrWhiteSpace()) return null;
        var entityRule = Navigations.GetByFinalName(propertyName) ?? BaseEntityRuleNode?.FindNavigationByFinalName(propertyName);
        return entityRule;
    }


    /// <summary> Return the navigation naming rule for the given navigation info </summary>
    public NavigationRuleNode TryResolveNavigationRuleFor(string fkName, Func<string> defaultEfName, bool thisIsPrincipal,
        bool isManyToMany, string inverseEntityName) {
        if ((Navigations == null || Navigations.Count == 0) && BaseEntityRuleNode == null) return null;

        if (isManyToMany) thisIsPrincipal = true; // many-to-many relationships always set IsPrincipal=true for both ends in the rule file.

        // locate by fkName first, which is most reliable.
        var navigations = fkName.HasNonWhiteSpace()
            ? GetNavigations()
                .Where(o => o.FkName == fkName && o.Rule.IsPrincipal == thisIsPrincipal)
                .ToArray()
            : Array.Empty<NavigationRuleNode>();

        IEnumerable<NavigationRuleNode> GetNavs() => navigations.Length > 0 ? navigations : GetNavigations();

        if (navigations.Length != 1) {
            // Maybe FkName is not defined or is inconsistent with DB.  Try to locate by expected name or inverse types
            if (inverseEntityName.HasNonWhiteSpace())
                navigations = GetNavs()
                    .Where(o => o.Rule.ToEntity == inverseEntityName && o.Rule.IsPrincipal == thisIsPrincipal)
                    .ToArray();

            if (navigations.Length != 1) {
                var efName = defaultEfName?.Invoke();
                if (efName.HasNonWhiteSpace()) {
                    navigations = GetNavs()?.Where(o => o.Rule.Name.EqualsIgnoreCase(efName)).ToArray();
                    if (navigations.IsNullOrEmpty()) return null; // expected EF name resolution failed to
                }
            }
        }

        return navigations!.Length != 1 ? null : navigations[0];
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public PropertyRuleNode AddProperty(IMutableProperty property, string column) {
        var rule = new PropertyRuleNode(new() {
            Name = column,
            PropertyName = property.Name
        }, this);
        Properties.Add(rule);
        Rule.Properties.Add(rule);
        return rule;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public NavigationRuleNode AddNavigation(IMutableNavigation navigation, string fkName, bool thisIsPrincipal, bool isManyToMany) {
        var rule = new NavigationRuleNode(new() {
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