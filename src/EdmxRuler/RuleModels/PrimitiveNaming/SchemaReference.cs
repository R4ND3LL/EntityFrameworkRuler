using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;

[DebuggerDisplay("Schema {SchemaName}")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class SchemaReference {
    public SchemaReference() {
        Tables = new List<ClassRename>();
    }

    [DataMember]
    public bool UseSchemaName { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string SchemaName { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<ClassRename> Tables { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string TableRegexPattern { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string TablePatternReplaceWith { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string ColumnRegexPattern { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string ColumnPatternReplaceWith { get; set; }

    /// <summary> Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously names classes. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false)]
    public string Namespace { get; set; }
}