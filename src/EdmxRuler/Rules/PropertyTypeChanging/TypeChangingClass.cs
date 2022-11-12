using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.Rules.PropertyTypeChanging;

[DebuggerDisplay("Class {Name}")]
[DataContract]
public sealed class TypeChangingClass : IEdmxRuleClassModel {
    /// <summary> The raw database name of the table. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string DbName { get; set; }

    /// <summary> The expected EF generated name for the entity. </summary>
    [DataMember(Order = 2)]
    public string Name { get; set; }

    /// <summary> The property rules to apply to this entity. </summary>

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public List<TypeChangingProperty> Properties { get; set; } = new();

    string IEdmxRuleClassModel.GetOldName() => Name;
    string IEdmxRuleClassModel.GetNewName() => Name;
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Properties;
}