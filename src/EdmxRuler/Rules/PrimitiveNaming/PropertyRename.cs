using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.Rules.PrimitiveNaming;

[DebuggerDisplay("Col {Name} to {NewName}")]
[DataContract]
public sealed class PropertyRename : IEdmxRulePropertyModel {
    /// <summary> The raw database name of the column. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string DbName { get; set; }

    /// <summary> The expected EF generated name for the property. </summary>
    [DataMember(Order = 2)]
    public string Name { get; set; }

    /// <summary> The new name to give the property. </summary>
    [DataMember(Order = 3)]
    public string NewName { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { Name };
    string IEdmxRulePropertyModel.GetNewName() => NewName;
    string IEdmxRulePropertyModel.GetNewTypeName() => null;
    NavigationMetadata IEdmxRulePropertyModel.GetNavigationMetadata() => default;
}