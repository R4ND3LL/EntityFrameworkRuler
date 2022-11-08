using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;

[DebuggerDisplay("Table {Name} to {NewName}")]
[DataContract]
public sealed class ClassRename : IEdmxRuleClassModel {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string NewName { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<PropertyRename> Columns { get; set; } = new();

    string IEdmxRuleClassModel.GetOldName() => Name;
    string IEdmxRuleClassModel.GetNewName() => NewName;
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Columns;
}