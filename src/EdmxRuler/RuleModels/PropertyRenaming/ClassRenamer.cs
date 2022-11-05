using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PropertyRenaming;

[DataContract]
public sealed class ClassRenamer {
    [DataMember]
    public string Name { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<PropertyRenamer> Properties { get; set; } = new();
}