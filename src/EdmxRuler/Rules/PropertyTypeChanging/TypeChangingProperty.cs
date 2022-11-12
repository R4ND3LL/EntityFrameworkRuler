using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.Rules.PropertyTypeChanging;

[DebuggerDisplay("Prop {Name} type {NewType}")]
[DataContract]
public sealed class TypeChangingProperty : IEdmxRulePropertyModel {
    /// <summary> The raw database name of the column. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string DbName { get; set; }

    /// <summary> The expected EF generated name for the property. </summary>
    [DataMember(Order = 1)]
    public string Name { get; set; }

    /// <summary> The new type to give the property. </summary>
    [DataMember(Order = 2)]
    public string NewType { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { Name };
    string IEdmxRulePropertyModel.GetNewName() => Name;
    string IEdmxRulePropertyModel.GetNewTypeName() => NewType;
    NavigationMetadata IEdmxRulePropertyModel.GetNavigationMetadata() => default;
}