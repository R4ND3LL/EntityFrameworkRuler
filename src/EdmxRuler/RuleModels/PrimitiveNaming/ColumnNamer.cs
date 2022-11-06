using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming; 

[DataContract]
public sealed class ColumnNamer {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string NewName { get; set; }
}