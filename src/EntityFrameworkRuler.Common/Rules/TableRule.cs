using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <inheritdoc />
[DebuggerDisplay("Table {Name} to {NewName}")]
[DataContract]
public sealed class TableRule : IClassRule {
    /// <summary> The raw database name of the table.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    public string Name { get; set; }

    /// <summary>
    /// The expected EF generated name for the entity.
    /// Used to locate the entity when applying rule after scaffolding using Roslyn.
    /// Usually only populated if different than the Name.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public string EntityName { get; set; }

    /// <summary> The new name to give the entity (if any). </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public string NewName { get; set; }

    /// <summary> If true, generate properties for columns that are not identified in this table rule.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 4)]
    public bool IncludeUnknownColumns { get; set; }

    /// <summary> Optional flag to omit this table during the scaffolding process. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    public bool NotMapped { get; set; }

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    internal bool Mapped => !NotMapped;

    /// <summary> The primitive property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    public List<ColumnRule> Columns { get; set; } = new();

    /// <summary> The navigation property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    public List<NavigationRule> Navigations { get; set; } = new();

    string IClassRule.GetOldName() => EntityName.CoalesceWhiteSpace(Name);
    string IClassRule.GetNewName() => NewName.CoalesceWhiteSpace(EntityName);

    IEnumerable<IPropertyRule> IClassRule.GetProperties() {
        if (!Columns.IsNullOrEmpty())
            foreach (var rule in Columns)
                yield return rule;
        if (!Navigations.IsNullOrEmpty())
            foreach (var rule in Navigations)
                yield return rule;
    }
}