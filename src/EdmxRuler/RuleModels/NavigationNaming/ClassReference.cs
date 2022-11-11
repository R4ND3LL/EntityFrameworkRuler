using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.NavigationNaming;

[DebuggerDisplay("Class {Name}")]
[DataContract]
public sealed class ClassReference : IEdmxRuleClassModel {
    [DataMember(Order = 1)]
    public string Name { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public List<NavigationRename> Properties { get; set; } = new();

    string IEdmxRuleClassModel.GetOldName() => Name;
    string IEdmxRuleClassModel.GetNewName() => Name;
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Properties;
}