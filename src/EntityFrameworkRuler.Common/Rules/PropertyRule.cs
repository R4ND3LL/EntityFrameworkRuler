using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Property rule </summary>
[DebuggerDisplay("Prop {Name} to {NewName}")]
[DataContract]
public sealed class PropertyRule : RuleBase, IPropertyRule {
    /// <inheritdoc />
    public PropertyRule() {
        DiscriminatorConditions = Observable ? new ObservableCollection<DiscriminatorCondition>() : new List<DiscriminatorCondition>();
    }

    /// <summary> The raw database name of the column.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("DB Name"), Category("Mapping"),
     Description("The raw database name of the column.  Used to locate the property during scaffolding phase.  Required."), Required]
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

    /// <summary> If using TPH inheritance, discriminator conditions describe the mapping from the base type to the concrete types. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    [DisplayName("Discriminator Conditions"), Category("TPH Configuration"),
     Description("If using TPH inheritance, discriminator conditions describe the mapping from the base type to the concrete types.")]
    public IList<DiscriminatorCondition> DiscriminatorConditions { get; }

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
    }
}

/// <summary> If using TPH, discriminator conditions describe the mapping from the base type to the concrete types. </summary>
[DebuggerDisplay("DiscriminatorCondition {Value} to {ToEntityName}")]
[DataContract]
public sealed class DiscriminatorCondition {
    /// <summary> Rows with this value will be mapped to the selected target entity.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("Value"), Category("Mapping"),
     Description("Rows with this value will be mapped to the selected target entity.  Required."), Required]
    public object Value { get; set; }

    /// <summary> The target concrete entity type.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 2)]
    [DisplayName("To Entity"), Category("Mapping"),
     Description("The target concrete entity type.  Required."), Required]
    public string ToEntityName { get; set; }
}