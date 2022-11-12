using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using EdmxRuler.Extensions;

namespace EdmxRuler.Rules.NavigationNaming;

[DebuggerDisplay("Class {Name}")]
[DataContract]
public sealed class ClassReference : IEdmxRuleClassModel {
    /// <summary>
    /// The raw database name of the table.  Used to aid in resolution of this rule instance during the scaffolding phase.
    /// Usually only populated when different from Name.
    /// </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
    public string DbName { get; set; }

    /// <summary> The expected EF generated name for the entity. Required. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 2)]
    public string Name { get; set; }

    /// <summary> The property rules to apply to this entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public List<NavigationRename> Properties { get; set; } = new();

    string IEdmxRuleClassModel.GetOldName() => Name.CoalesceWhiteSpace(DbName);
    string IEdmxRuleClassModel.GetNewName() => Name;
    IEnumerable<IEdmxRulePropertyModel> IEdmxRuleClassModel.GetProperties() => Properties;
}