using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Schema rule </summary>
[DebuggerDisplay("Schema {SchemaName}")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class SchemaRule {
    /// <summary> Creates a schema rule </summary>
    public SchemaRule() {
        Tables = new();
    }

    /// <summary> The schema name this rule applies to.  Required. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string SchemaName { get; set; }

    /// <summary> Prefix entity names with the schema name </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public bool UseSchemaName { get; set; }

    /// <summary> If true, generate entity models for simple many-to-many junctions rather than suppressing them automatically.  Default is false. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public bool UseManyToManyEntity { get; set; }

    /// <summary> If true, generate entity models for tables that are not identified in this schema rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 4)]
    public bool IncludeUnknownTables { get; set; }

    /// <summary> If true, generate entity models for views that are not identified in this schema rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 5)]
    public bool IncludeUnknownViews { get; set; }

    /// <summary> Optional flag to omit this schema and all tables within it during the scaffolding process. Default is false. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    public bool NotMapped { get; set; }

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    internal bool Mapped => !NotMapped;

    /// <summary> Table regex pattern </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    public string TableRegexPattern { get; set; }

    /// <summary> Table pattern replace with </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 8)]
    public string TablePatternReplaceWith { get; set; }

    /// <summary> Column regex pattern </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 9)]
    public string ColumnRegexPattern { get; set; }

    /// <summary> Column pattern replace with </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 10)]
    public string ColumnPatternReplaceWith { get; set; }

    /// <summary> Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously named classes. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 11)]
    public string Namespace { get; set; }

    /// <summary> Table rules </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 12)]
    public List<TableRule> Tables { get; set; }
}