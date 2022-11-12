using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using EdmxRuler.Extensions;

namespace EdmxRuler.Rules.PrimitiveNaming;

[DebuggerDisplay("Table {Name} to {NewName}")]
[DataContract]
public sealed class TableRename : IEdmxRuleClassModel {
    /// <summary> The raw database name of the table.  Used to locate the property during scaffolding phase.  Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    public string Name { get; set; }

    /// <summary>
    /// The expected EF generated name for the entity.
    /// Used to locate the entity when applying rule after scaffolding using Roslyn.
    /// Usually only populated if different than the Name.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public string EntityName { get; set; }

    /// <summary> The new name to give the entity (if any). </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public string NewName { get; set; }

    /// <summary> The property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public List<ColumnRename> Columns { get; set; } = new();

    string IEdmxRuleClassModel.GetOldName() => EntityName.CoalesceWhiteSpace(Name);
    string IEdmxRuleClassModel.GetNewName() => NewName.CoalesceWhiteSpace(EntityName);
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Columns;
}