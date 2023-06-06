using EntityFrameworkRuler.Design.Metadata.Builders;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class FunctionRuleNode : RuleNode<FunctionRule, SchemaRuleNode> {
    /// <inheritdoc />
    public FunctionRuleNode(FunctionRule r, SchemaRuleNode parent) : base(r, parent) {
        Parameters = new(() => r.Parameters.Select(o => new FunctionParameterRuleNode(o, this)), parent.Parent.Rule.CaseSensitive);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DbContextRuleNode DbContextRuleNode => Parent?.Parent;


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    private RuleIndex<FunctionParameterRuleNode> Parameters { get; }

    /// <summary> Get properties recursively </summary>
    public IEnumerable<FunctionParameterRuleNode> GetParameters() => Parameters;

    /// <summary> sum of local properties and navigations </summary>
    public int LocalParameterCount => Parameters.Count + Navigations.Count;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    private RuleIndex<NavigationRuleNode> Navigations { get; }


    /// <summary> The underlying table for this entity.  May or may not have been omitted yet. </summary>
    public ScaffoldedTableTrackerItem ScaffoldedTable { get; private set; }

    /// <summary> The entity type builder used to scaffold this table.  Presence of this value implies the entity was not omitted. </summary>
    public FunctionBuilder Builder { get; private set; }

    /// <summary> True if this entity has been scaffolded with an entity builder.
    /// Note, will be false if table was identified but entity omitted. </summary>
    public bool IsAlreadyScaffolded => !IsOmitted && Builder != null && ScaffoldedTable != null;

    /// <summary> True if this entity has been mapped to a database table (or view).  May or may not have been omitted. </summary>
    public bool IsMappedToTable => ScaffoldedTable != null;

    /// <inheritdoc />
    public override string GetFinalName() => Builder?.Metadata.Name ?? base.GetFinalName();

    // /// <summary> Link this entity rule to the scaffolded function builder. </summary>
    // public void MapTo(FunctionBuilder builder, ScaffoldedTableTrackerItem scaffoldedTable) {
    //     Debug.Assert(Builder == null, "Builder was previously set");
    //     Debug.Assert(Rule.ShouldMap(), "Function should not be scaffolded");
    //     if (ScaffoldedTable == null) {
    //         if (scaffoldedTable != null) MapTo(scaffoldedTable);
    //         if (ScaffoldedTable == null) throw new("Cannot set entity builder without ScaffoldedTable");
    //     } else {
    //         if (scaffoldedTable != null && !ReferenceEquals(ScaffoldedTable, scaffoldedTable))
    //             throw new("ScaffoldedTable instance mismatch");
    //     }
    //
    //     Builder = builder;
    //     Parent.Parent.Map(builder, this);
    //     UpdateRuleMetadata();
    // }

    // /// <summary> Link this function rule to the underlying database function.  This will be linked whether the function is omitted or not. </summary>
    // public void MapTo(ScaffoldedTableTrackerItem table) {
    //     Debug.Assert(ScaffoldedTable == null);
    //     ScaffoldedTable = table;
    //     table.MapTo(this);
    //     UpdateRuleMetadata();
    // }

    private void UpdateRuleMetadata() {
        if (ScaffoldedTable == null) return;
        Rule.Name = ScaffoldedTable.Name;
        if (Builder == null) return;

        // // can only update expected name if it wasn't already influenced by dynamic naming or NewName
        // if (Rule.NewName.IsNullOrWhiteSpace() && !Parent.IsDynamicNamingTables &&
        //     (Rule.FunctionName.HasNonWhiteSpace() || Builder.Metadata.Name != (ScaffoldedTable?.Name ?? Rule.Name)))
        //     Rule.FunctionName = Builder.Metadata.Name;
    }


    /// <summary> Get the property rule for the given target parameter. Used during scaffolding phase. </summary>
    public FunctionParameterRuleNode TryResolveRuleFor(string parameter) {
        if (parameter.IsNullOrWhiteSpace()) return null;

        var entityRule = FindParameterByDbName(parameter);
        if (entityRule != null) return entityRule;

        entityRule = FindParameterByFinalName(parameter);
        if (entityRule?.DbName.HasNonWhiteSpace() == true) entityRule = null;

        return entityRule;
    }

    /// <summary> Get the property rule recursively for the given target parameter. Used during scaffolding phase. </summary>
    public FunctionParameterRuleNode FindParameterByDbName(string column) {
        if (column.IsNullOrWhiteSpace()) return null;
        var entityRule = Parameters.GetByDbName(column);
        return entityRule;
    }

    /// <summary> Get the property rule recursively for the given target property name. Used during scaffolding phase. </summary>
    public FunctionParameterRuleNode FindParameterByFinalName(string propertyName) {
        if (propertyName.IsNullOrWhiteSpace()) return null;
        var entityRule = Parameters.GetByFinalName(propertyName);
        return entityRule;
    }

    /// <summary> Get the navigation rule recursively for the given target property name. Used during scaffolding phase. </summary>
    public NavigationRuleNode FindNavigationByFinalName(string propertyName) {
        if (propertyName.IsNullOrWhiteSpace()) return null;
        var entityRule = Navigations.GetByFinalName(propertyName);
        return entityRule;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public FunctionParameterRuleNode AddParameter(string finalName, string dbName) {
        var rule = new FunctionParameterRuleNode(new() {
            Name = dbName,
            NewName = finalName
        }, this);
        Parameters.Add(rule);
        Rule.Parameters.Add(rule);
        return rule;
    }
 
    /// <summary> implicit conversion </summary>
    public static implicit operator FunctionRule(FunctionRuleNode o) => o?.Rule;
}