using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.EnumMapping; 

[DataContract]
public sealed class EnumMappingProperty {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string EnumType { get; set; }
}