using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;

[DebuggerDisplay("Table {Name} to {NewName}")]
[DataContract]
public sealed class ClassRename : IEdmxRuleClassModel {
    [DataMember(Order = 1)]
    public string Name { get; set; }

    [DataMember(Order = 2)]
    public string NewName { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public List<PropertyRename> Columns { get; set; } = new();

    string IEdmxRuleClassModel.GetOldName() => Name;
    string IEdmxRuleClassModel.GetNewName() => NewName;
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Columns;
}