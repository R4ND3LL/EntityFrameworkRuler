using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;
[DebuggerDisplay("Schema {Name}")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class Schema {
    public Schema() {
        Tables = new List<TableRenamer>();
    }

    [DataMember]
    public bool UseSchemaName { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string SchemaName { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<TableRenamer> Tables { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string TableRegexPattern { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string TablePatternReplaceWith { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string ColumnRegexPattern { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string ColumnPatternReplaceWith { get; set; }
}