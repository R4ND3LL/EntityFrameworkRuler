using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.Rules.PrimitiveNaming;

[DebuggerDisplay("Table {Name} to {NewName}")]
[DataContract]
public sealed class ClassRename : IEdmxRuleClassModel {
    /// <summary> The raw database name of the table. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string DbName { get; set; }

    /// <summary> The expected EF generated name for the entity. </summary>
    [DataMember(Order = 2)]
    public string Name { get; set; }

    /// <summary> The new name to give the entity. </summary>
    [DataMember(Order = 3)]
    public string NewName { get; set; }

    /// <summary> The property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public List<PropertyRename> Columns { get; set; } = new();

    string IEdmxRuleClassModel.GetOldName() => Name;
    string IEdmxRuleClassModel.GetNewName() => NewName;
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Columns;
}