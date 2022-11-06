using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.EnumMapping;
[DebuggerDisplay("Prop {Name} enum type {EnumType}")]
[DataContract]
public sealed class EnumMappingProperty {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string EnumType { get; set; }
}