using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EdmxRuler.Extensions;

namespace EdmxRuler.RuleModels.NavigationNaming;

[DebuggerDisplay("Nav {FirstName} to {NewName}")]
[DataContract]
public sealed class NavigationRename : IEdmxRulePropertyModel {
    /// <summary>
    /// Gets or sets the name alternatives to look for when identifying this property.
    /// Used in navigation renaming since prediction of the reverse engineered name can be difficult.
    /// This way, for example, the user can supply options using the concatenated form like "Fk+Navigation(s)"
    /// as well as the basic pluralized form.
    /// </summary>
    [DataMember]
    public HashSet<string> Name { get; set; } = new(2);

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    internal string FirstName => Name?.FirstOrDefault();

    [DataMember]
    public string NewName { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() =>
        Name?.Where(o => o.HasNonWhiteSpace()).Distinct() ?? Array.Empty<string>();

    string IEdmxRulePropertyModel.GetNewName() => NewName;
    string IEdmxRulePropertyModel.GetNewTypeName() => null;
}