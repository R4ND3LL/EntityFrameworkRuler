using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.Rules.PropertyTypeChanging;

[DebuggerDisplay("Class {Name}")]
[DataContract]
public sealed class TypeChangingClass : IEdmxRuleClassModel {
    /// <summary>
    /// The database schema name that the entity table is derived from.
    /// Used to aid in resolution of this rule instance during the scaffolding phase. Not used in Roslyn application.
    /// Optional.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string DbSchema { get; set; }

    /// <summary> The raw database name of the table.  Optional. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public string DbName { get; set; }

    /// <summary> The expected EF generated name for the entity.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 3)]
    public string Name { get; set; }

    /// <summary> The property rules to apply to this entity. </summary>

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public List<TypeChangingProperty> Properties { get; set; } = new();

    string IEdmxRuleClassModel.GetOldName() => Name;
    string IEdmxRuleClassModel.GetNewName() => Name;
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Properties;
}