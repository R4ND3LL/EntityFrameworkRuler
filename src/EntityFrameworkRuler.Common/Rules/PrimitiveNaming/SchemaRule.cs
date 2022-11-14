using System.Diagnostics;
using System.Runtime.Serialization;

namespace EntityFrameworkRuler.Rules.PrimitiveNaming;

[DebuggerDisplay("Schema {SchemaName}")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class SchemaRule {
    public SchemaRule() {
        Tables = new();
    }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string SchemaName { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public bool UseSchemaName { get; set; }

    /// <summary> If true, generate entity models for simple many-to-many junctions rather than suppressing them automatically.  Default is false. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public bool UseManyToManyEntity { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public string TableRegexPattern { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    public string TablePatternReplaceWith { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    public string ColumnRegexPattern { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    public string ColumnPatternReplaceWith { get; set; }

    /// <summary> Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously named classes. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 8)]
    public string Namespace { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 9)]
    public List<TableRule> Tables { get; set; }
}