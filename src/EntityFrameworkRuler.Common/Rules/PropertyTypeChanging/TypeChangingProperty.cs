using System.Diagnostics;
using System.Runtime.Serialization;

namespace EntityFrameworkRuler.Rules.PropertyTypeChanging;

[DebuggerDisplay("Prop {PropertyName} type {NewType}")]
[DataContract]
public sealed class TypeChangingProperty : IEdmxRulePropertyModel {
    /// <summary> The raw database name of the column.  Optional. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string Name { get; set; }

    /// <summary> The expected EF generated name for the property.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 2)]
    public string PropertyName { get; set; }

    /// <summary> The new type to give the property. </summary>
    [DataMember(Order = 3)]
    public string NewType { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { PropertyName, Name };
    string IEdmxRulePropertyModel.GetNewName() => PropertyName;
    string IEdmxRulePropertyModel.GetNewTypeName() => NewType;
    NavigationMetadata IEdmxRulePropertyModel.GetNavigationMetadata() => default;
}