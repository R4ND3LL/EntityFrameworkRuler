using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Schema rule </summary>
[DebuggerDisplay("Schema {Name}")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class SchemaRule : RuleBase, ISchemaRule {
    /// <summary> Creates a schema rule </summary>
    public SchemaRule() {
        Tables = new();
    }

    /// <summary> The schema name this rule applies to.  Required. </summary>
    [DataMember(Name = "SchemaName", EmitDefaultValue = false, IsRequired = false, Order = 1)]
    [DisplayName("Name"), Category("Mapping"), Description("The schema name this rule applies to.  Required."), Required]
    public string Name { get; set; }

    /// <summary> If true, generate entities for simple many-to-many junctions rather than letting EF suppress them.  Default is false. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    [DisplayName("Use Many-to-Many Entities"), Category("Mapping"), Description("If true, generate entities for simple many-to-many junctions rather than letting EF suppress them.  Default is false.")]
    public bool UseManyToManyEntity { get; set; }

    /// <summary> If true, generate entities  for tables that are not identified in this schema rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 3)]
    [DisplayName("Include Unknown Views"), Category("Mapping"), Description("If true, generate entities for views that are not identified in this schema rule.  Default is false.")]
    public bool IncludeUnknownTables { get; set; }

    /// <summary> If true, generate entities for views that are not identified in this schema rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 4)]
    [DisplayName("Include Unknown Views"), Category("Mapping"), Description("If true, generate entities for views that are not identified in this schema rule.  Default is false.")]
    public bool IncludeUnknownViews { get; set; }



    /// <summary> If true, omit this schema and all tables within it during the scaffolding process. Default is false. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this table during the scaffolding process.")]
    public override bool NotMapped { get; set; }

    /// <summary> Prefix entity names with the schema name. Only done when the name is not explicitly identified herein for the given entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    [DisplayName("Use Schema Name"), Category("Naming"), Description("Prefix entity names with the schema name. Only done when the name is not explicitly identified herein for the given entity.")]
    public bool UseSchemaName { get; set; }


    /// <summary> Table regex pattern </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    [DisplayName("Table Regex Pattern"), Category("Naming"), Description("Table regex pattern.")]
    public string TableRegexPattern { get; set; }

    /// <summary> Table pattern replace with </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 8)]
    [DisplayName("Table Pattern Replace With"), Category("Naming"), Description("Table pattern replace with.")]
    public string TablePatternReplaceWith { get; set; }

    /// <summary> Column regex pattern </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 9)]
    [DisplayName("Column Regex Pattern"), Category("Naming"), Description("Column regex pattern.")]
    public string ColumnRegexPattern { get; set; }

    /// <summary> Column pattern replace with </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 10)]
    [DisplayName("Column Pattern Replace With"), Category("Naming"), Description("Column pattern replace with.")]
    public string ColumnPatternReplaceWith { get; set; }

    /// <summary> Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously named classes. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 11)]
    [DisplayName("Namespace"), Category("Mapping"), Description("Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously named classes.")]
    public string Namespace { get; set; }

    /// <summary> The table rules to apply to this schema. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 12)]
    [DisplayName("Tables"), Category("Tables|Tables"), Description("The table rules to apply to this schema.")]
    public List<TableRule> Tables { get; set; }

    /// <inheritdoc />
    protected override string GetNewName() => null;
    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => Name;
    /// <inheritdoc />
    protected override void SetFinalName(string value) {
        Name = value;
        //OnPropertyChanged(nameof(Name));
    }
    IEnumerable<IClassRule> ISchemaRule.GetClasses() => Tables;
}