using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <inheritdoc />
[DebuggerDisplay("Col {Name} to {NewName}")]
[DataContract]
public sealed class ColumnRule : IPropertyRule {
    /// <summary> The raw database name of the column.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    public string Name { get; set; }

    /// <summary>
    /// The expected EF generated name for the property.
    /// Used to locate the property when applying rule after scaffolding using Roslyn.
    /// Usually only populated if different than the Name.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public string PropertyName { get; set; }

    /// <summary> The new name to give the property. Optional. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public string NewName { get; set; }

    /// <summary> The new type to give the property. Optional. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public string NewType { get; set; }

    /// <summary> Optional flag to omit this column during the scaffolding process. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    public bool NotMapped { get; set; }

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    internal bool Mapped => !NotMapped;

    IEnumerable<string> IPropertyRule.GetCurrentNameOptions() => new[] { PropertyName, Name };
    string IPropertyRule.GetNewName() => NewName;
    string IPropertyRule.GetNewTypeName() => null;
    NavigationMetadata IPropertyRule.GetNavigationMetadata() => default;
}