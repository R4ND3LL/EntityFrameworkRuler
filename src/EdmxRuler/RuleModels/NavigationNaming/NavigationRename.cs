using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using EdmxRuler.Extensions;

namespace EdmxRuler.RuleModels.NavigationNaming;

[DebuggerDisplay("Nav {Name} to {NewName}")]
[DataContract]
public sealed class NavigationRename : IEdmxRulePropertyModel {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string NewName { get; set; }

    /// <summary>
    /// Gets or sets the optional alternative name to look for if Name is not found.
    /// Used in navigation renaming since prediction of the generated name can be difficult.
    /// This way, for example, the user can use Name to suggest the "Fk+Navigation(s)" name while
    /// AlternateName supplies the basic pluralization name.
    /// </summary>
    [DataMember]
    public string AlternateName { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() =>
        new[] { Name, AlternateName }.Where(o => o.HasNonWhiteSpace()).Distinct();

    string IEdmxRulePropertyModel.GetNewName() => NewName;
    string IEdmxRulePropertyModel.GetNewTypeName() => null;
}