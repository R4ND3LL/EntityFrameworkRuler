using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;

[DebuggerDisplay("Col {Name} to {NewName}")]
[DataContract]
public sealed class PropertyRename : IEdmxRulePropertyModel {
    [DataMember(Order = 1)]
    public string Name { get; set; }

    [DataMember(Order = 2)]
    public string NewName { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { Name };
    string IEdmxRulePropertyModel.GetNewName() => NewName;
    string IEdmxRulePropertyModel.GetNewTypeName() => null;
    NavigationMetadata IEdmxRulePropertyModel.GetNavigationMetadata() => default;
}