using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.NavigationNaming;

[DataContract]
public sealed class ClassReference {
    [DataMember]
    public string Name { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<NavigationRename> Properties { get; set; } = new();
}