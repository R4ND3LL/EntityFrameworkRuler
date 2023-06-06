using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Schema rule </summary>
[DebuggerDisplay("Schema {SchemaName}")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class SchemaRule : RuleBase, ISchemaRule {
    /// <summary> Creates a schema rule </summary>
    public SchemaRule() {
        entities = Observable ? new ObservableCollection<EntityRule>() : new List<EntityRule>();
        functions = Observable ? new ObservableCollection<FunctionRule>() : new List<FunctionRule>();
    }

    /// <summary> The schema name this rule applies to.  Required. </summary>
    [DataMember(Name = "SchemaName", EmitDefaultValue = false, IsRequired = false, Order = 1)]
    [DisplayName("Name"), Category("Mapping"), Description("The schema name this rule applies to.  Required."), Required]
    public string SchemaName { get; set; }

    /// <summary> If true, generate entities for simple many-to-many junctions rather than letting EF suppress them.  Default is false. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    [DisplayName("Use Many-to-Many Entities"), Category("Mapping"),
     Description("If true, generate entities for simple many-to-many junctions rather than letting EF suppress them.  Default is false.")]
    public bool UseManyToManyEntity { get; set; }

    /// <summary> If true, generate entities  for tables that are not identified in this schema rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 3)]
    [DisplayName("Include Unknown Views"), Category("Mapping"),
     Description("If true, generate entities for views that are not identified in this schema rule.  Default is false.")]
    public bool IncludeUnknownTables { get; set; }

    /// <summary> If true, generate entities for views that are not identified in this schema rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 4)]
    [DisplayName("Include Unknown Views"), Category("Mapping"),
     Description("If true, generate entities for views that are not identified in this schema rule.  Default is false.")]
    public bool IncludeUnknownViews { get; set; }


    /// <summary> If true, omit this schema and all tables within it during the scaffolding process. Default is false. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this table during the scaffolding process.")]
    public bool NotMapped { get; set; }


    /// <summary> Prefix entity names with the schema name. Only done when the name is not explicitly identified herein for the given entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    [DisplayName("Use Schema Name"), Category("Naming"),
     Description(
         "Prefix entity names with the schema name. Only done when the name is not explicitly identified herein for the given entity.")]
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

    /// <summary> Function regex pattern </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 11)]
    [DisplayName("Function Regex Pattern"), Category("Naming"), Description("Function regex pattern.")]
    public string FunctionRegexPattern { get; set; }

    /// <summary> Function pattern replace with </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 12)]
    [DisplayName("Function Pattern Replace With"), Category("Naming"), Description("Function pattern replace with.")]
    public string FunctionPatternReplaceWith { get; set; }
    
    /// <summary> Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously named classes. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 13)]
    [DisplayName("Namespace"), Category("Mapping"),
     Description(
         "Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously named classes.")]
    public string Namespace { get; set; }

    private IList<EntityRule> entities;

    /// <summary> The table rules to apply to this schema. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 14)]
    [DisplayName("Entities"), Category("Entities|Entities"), Description("The entity rules to apply to this schema.")]
    public IList<EntityRule> Entities {
        get => entities;
        set => UpdateCollection(ref entities, value);
    }

    /// <summary> Serialization backward compatibility for Tables -> Entities. </summary>
    [Obsolete("Use Entities instead"), Browsable(false)]
    // ReSharper disable once UnusedMember.Global
    public IList<EntityRule> Tables { get => Entities; set => Entities = value; }
    
    private IList<FunctionRule> functions;

    /// <summary> The function rules to apply to this schema. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 15)]
    [DisplayName("Functions"), Category("Functions|Functions"), Description("The function rules to apply to this schema.")]
    public IList<FunctionRule> Functions {
        get => functions;
        set => UpdateCollection(ref functions, value);
    }

    /// <inheritdoc />
    protected override string GetDbName() => SchemaName.EmptyIfNullOrWhitespace();

    /// <inheritdoc />
    protected override string GetNewName() => null;

    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => SchemaName;

    /// <inheritdoc />
    protected override void SetFinalName(string value) {
        SchemaName = value.EmptyIfNullOrWhitespace();
        //OnPropertyChanged(nameof(Name));
    }

    /// <inheritdoc />
    protected override bool GetNotMapped() => NotMapped;

    IEnumerable<IEntityRule> ISchemaRule.GetClasses() => Entities;
}