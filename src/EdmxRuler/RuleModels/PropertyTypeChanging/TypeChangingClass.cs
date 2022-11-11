using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PropertyTypeChanging;

[DebuggerDisplay("Class {Name}")]
[DataContract]
public sealed class TypeChangingClass : IEdmxRuleClassModel {
    public TypeChangingClass() {
        Properties = new List<TypeChangingProperty>();
    }

    [DataMember(Order = 1)]
    public string Name { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public List<TypeChangingProperty> Properties { get; set; }

    string IEdmxRuleClassModel.GetOldName() => Name;
    string IEdmxRuleClassModel.GetNewName() => Name;
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Properties;
}