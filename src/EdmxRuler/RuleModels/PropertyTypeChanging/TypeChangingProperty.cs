using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PropertyTypeChanging;

[DebuggerDisplay("Prop {Name} type {NewType}")]
[DataContract]
public sealed class TypeChangingProperty : IEdmxRulePropertyModel {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string NewType { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { Name };
    string IEdmxRulePropertyModel.GetNewName() => Name;
    string IEdmxRulePropertyModel.GetNewTypeName() => NewType;
}