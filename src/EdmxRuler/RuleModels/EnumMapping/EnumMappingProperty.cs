using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.EnumMapping;

[DebuggerDisplay("Prop {Name} enum type {EnumType}")]
[DataContract]
public sealed class EnumMappingProperty : IEdmxRulePropertyModel {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string EnumType { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { Name };
    string IEdmxRulePropertyModel.GetNewName() => Name;
    string IEdmxRulePropertyModel.GetNewTypeName() => EnumType;
}