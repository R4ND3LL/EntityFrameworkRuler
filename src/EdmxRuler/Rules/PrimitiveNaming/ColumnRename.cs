using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EdmxRuler.Rules.PrimitiveNaming;

[DebuggerDisplay("Col {Name} to {NewName}")]
[DataContract]
public sealed class ColumnRename : IEdmxRulePropertyModel {
    /// <summary> The raw database name of the column.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    public string Name { get; set; }

    /// <summary>
    /// The expected EF generated name for the property.
    /// Used to locate the property when applying rule after scaffolding using Roslyn.
    /// Usually only populated if different than the Name.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public string PropertyName { get; set; }

    /// <summary> The new name to give the property. </summary>
    [DataMember(Order = 3)]
    public string NewName { get; set; }

    /// <summary> Optional flag to suppress this column in the scaffolding process. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public bool NotMapped { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() => new[] { PropertyName, Name };
    string IEdmxRulePropertyModel.GetNewName() => NewName;
    string IEdmxRulePropertyModel.GetNewTypeName() => null;
    NavigationMetadata IEdmxRulePropertyModel.GetNavigationMetadata() => default;
}