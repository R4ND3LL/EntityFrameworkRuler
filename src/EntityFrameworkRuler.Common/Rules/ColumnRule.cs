using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <inheritdoc />
[DebuggerDisplay("Col {Name} to {NewName}")]
[DataContract]
public sealed class ColumnRule : RuleBase, IPropertyRule {
    /// <summary> The raw database name of the column.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("DB Name"), Category("Mapping"), Description("The raw database name of the column.  Used to locate the property during scaffolding phase.  Required."), Required]
    public string Name { get; set; }

    /// <summary>
    /// The expected EF generated name for the property.
    /// Used to locate the property when applying rule after scaffolding using Roslyn.
    /// Usually only populated if different than the Name.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    [DisplayName("Expected Property Name"), Category("Mapping"), Description("The expected EF generated name for the property.")]
    public string PropertyName { get; set; }

    /// <summary> The new name to give the property. Optional. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    [DisplayName("New Name"), Category("Mapping"), Description("The new name to give the property. Optional.")]
    public string NewName { get; set; }

    /// <summary> The new type to give the property. Optional. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    [DisplayName("New Type"), Category("Mapping"), Description("The new type to give the property. Optional.")]
    public string NewType { get; set; }

    /// <summary> If true, omit this column during the scaffolding process. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this table during the scaffolding process.")]
    public override bool NotMapped { get; set; }

    IEnumerable<string> IPropertyRule.GetCurrentNameOptions() => new[] { PropertyName, Name };
    string IPropertyRule.GetNewTypeName() => NewType;
    NavigationMetadata IPropertyRule.GetNavigationMetadata() => default;

    /// <inheritdoc />
    protected override string GetNewName() => NewName.NullIfEmpty();
    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => PropertyName.NullIfEmpty() ?? Name;

    /// <inheritdoc />
    protected override void SetFinalName(string value) {
        NewName = value;
        //OnPropertyChanged(nameof(NewName));
    }

}