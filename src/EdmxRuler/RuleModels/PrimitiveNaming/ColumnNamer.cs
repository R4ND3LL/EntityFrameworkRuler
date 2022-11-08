using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;

[DebuggerDisplay("Col {Name} to {NewName}")]
[DataContract]
public sealed class ColumnNamer : IEdmxRulePropertyModel {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string NewName { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { Name };
    string IEdmxRulePropertyModel.GetNewName() => NewName;
    string IEdmxRulePropertyModel.GetNewTypeName() => null;
}