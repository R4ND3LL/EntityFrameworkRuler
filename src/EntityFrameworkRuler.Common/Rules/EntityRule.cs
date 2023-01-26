using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Table rule </summary>
[DebuggerDisplay("Entity {NewName} : {BaseTypeName} (from table {Name})")]
[DataContract]
public sealed class EntityRule : RuleBase, IEntityRule {
    /// <summary> Creates a table rule </summary>
    public EntityRule() {
        navigations = Observable ? new ObservableCollection<NavigationRule>() : new List<NavigationRule>();
        properties = Observable ? new ObservableCollection<PropertyRule>() : new List<PropertyRule>();
    }

    /// <summary> The raw database name of the table.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("DB Name"), Category("Mapping"), Description("The storage name of the table."), Required]
    public string Name { get; set; }

    /// <summary>
    /// The expected EF generated name for the entity.
    /// Used to locate the entity when applying rules after scaffolding using Roslyn.
    /// Usually only populated if different than the Name.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    [DisplayName("Expected Entity Name"), Category("Mapping"), Description("The expected EF reverse engineered name of the table.")]
    public string EntityName { get; set; }

    /// <summary> The new name to give the entity (if any). </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    [DisplayName("New Name"), Category("Mapping"), Description("The new name to give the entity.")]
    public string NewName { get; set; }

    /// <summary> The DB Set name to use for the entity collection within the DB context.  The default behavior is to pluralize the entity name. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    [DisplayName("DBSet Name"), Category("Mapping"),
     Description(
         "The DB Set name to use for the entity collection within the DB context.  The default behavior is to pluralize the entity name.")]
    public string DbSetName { get; set; }

    /// <summary> The base entity type name use in an inheritance strategy. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    [DisplayName("Base Type"), Category("Mapping"), Description("The base entity type name use in an inheritance strategy.")]
    public string BaseTypeName { get; set; }

    /// <summary> If true, generate properties for columns that are not identified in this table rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 6)]
    [DisplayName("Include Unknown Columns"), Category("Mapping"),
     Description("If true, generate properties for columns that are not identified in this table rule.  Default is false.")]
    public bool IncludeUnknownColumns { get; set; }

    /// <summary> If true, omit this table during the scaffolding process. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this table during the scaffolding process.")]
    public bool NotMapped { get; set; }


    private IList<PropertyRule> properties;

    /// <summary> The primitive property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 8)]
    [DisplayName("Properties"), Category("Properties|Properties"), Description("The primitive property rules to apply to this entity.")]
    public IList<PropertyRule> Properties {
        get => properties;
        set => UpdateCollection(ref properties, value);
    }

    /// <summary> Serialization backward compatibility for Columns -> Properties. </summary>
    [Obsolete("Use Properties instead"), Browsable(false)]
    public IList<PropertyRule> Columns { get => Properties; set => Properties = value; }

    private IList<NavigationRule> navigations;

    /// <summary> The navigation property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 9)]
    [DisplayName("Navigations"), Category("Navigations|Navigations"), Description("The navigation property rules to apply to this entity.")]
    public IList<NavigationRule> Navigations {
        get => navigations;
        set => UpdateCollection(ref navigations, value);
    }

    /// <inheritdoc />
    protected override string GetDbName() => Name;

    /// <inheritdoc />
    protected override string GetNewName() => NewName.NullIfEmpty();

    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => EntityName.NullIfEmpty() ?? Name.NullIfEmpty();

    /// <inheritdoc />
    protected override void SetFinalName(string value) {
        NewName = value;
        //OnPropertyChanged(nameof(NewName));
    }

    /// <inheritdoc />
    protected override bool GetNotMapped() => NotMapped;

    IEnumerable<IPropertyRule> IEntityRule.GetProperties() {
        if (!Properties.IsNullOrEmpty())
            foreach (var rule in Properties)
                yield return rule;
        if (!Navigations.IsNullOrEmpty())
            foreach (var rule in Navigations)
                yield return rule;
    }
}