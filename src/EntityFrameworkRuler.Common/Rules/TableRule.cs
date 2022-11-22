using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <inheritdoc />
[DebuggerDisplay("Table {Name} to {NewName}")]
[DataContract]
public sealed class TableRule : RuleBase, IClassRule {
    /// <summary> The raw database name of the table.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("DB Name"), Category("Mapping"), Description("The storage name of the table."), Required]
    public string Name { get; set; }


    /// <summary>
    /// The expected EF generated name for the entity.
    /// Used to locate the entity when applying rule after scaffolding using Roslyn.
    /// Usually only populated if different than the Name.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    [DisplayName("Expected Entity Name"), Category("Mapping"), Description("The expected EF reverse engineered name of the table.")]
    public string EntityName { get; set; }

    /// <summary> The new name to give the entity (if any). </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    [DisplayName("New Name"), Category("Mapping"), Description("The new name to give the entity.")]
    public string NewName { get; set; }

    /// <summary> If true, generate properties for columns that are not identified in this table rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 4)]
    [DisplayName("Include Unknown Columns"), Category("Mapping"), Description("If true, generate properties for columns that are not identified in this table rule.  Default is false.")]
    public bool IncludeUnknownColumns { get; set; }

    /// <summary> If true, omit this table during the scaffolding process. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this table during the scaffolding process.")]
    public override bool NotMapped { get; set; }

    /// <summary> The primitive property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    [DisplayName("Properties"), Category("Properties|Properties"), Description("The primitive property rules to apply to this entity.")]
    public List<ColumnRule> Columns { get; set; } = new();

    /// <summary> The navigation property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    [DisplayName("Navigations"), Category("Navigations|Navigations"), Description("The navigation property rules to apply to this entity.")]
    public List<NavigationRule> Navigations { get; set; } = new();

    /// <inheritdoc />
    protected override string GetNewName() => NewName.NullIfEmpty();
    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => EntityName.NullIfEmpty() ?? Name.NullIfEmpty();
    /// <inheritdoc />
    protected override void SetFinalName(string value) {
        NewName = value;
        OnPropertyChanged(nameof(NewName));
    }

    IEnumerable<IPropertyRule> IClassRule.GetProperties() {
        if (!Columns.IsNullOrEmpty())
            foreach (var rule in Columns)
                yield return rule;
        if (!Navigations.IsNullOrEmpty())
            foreach (var rule in Navigations)
                yield return rule;
    }
}