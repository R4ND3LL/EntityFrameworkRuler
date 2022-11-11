using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PropertyTypeChanging;

[DebuggerDisplay("Prop {Name} type {NewType}")]
[DataContract]
public sealed class TypeChangingProperty : IEdmxRulePropertyModel {
    [DataMember(Order = 1)]
    public string Name { get; set; }

    [DataMember(Order = 2)]
    public string NewType { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { Name };
    string IEdmxRulePropertyModel.GetNewName() => Name;
    string IEdmxRulePropertyModel.GetNewTypeName() => NewType;
    NavigationMetadata IEdmxRulePropertyModel.GetNavigationMetadata() => default;
}