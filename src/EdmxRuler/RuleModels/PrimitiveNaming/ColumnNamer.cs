using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;
[DebuggerDisplay("Col {Name} to {NewName}")]
[DataContract]
public sealed class ColumnNamer {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string NewName { get; set; }
}