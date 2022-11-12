using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EdmxRuler.Extensions;

namespace EdmxRuler.RuleModels.NavigationNaming;

[DebuggerDisplay("Nav {FirstName} to {NewName} FkName={FkName} IsPrincipal={IsPrincipal}")]
[DataContract]
public sealed class NavigationRename : IEdmxRulePropertyModel {
    /// <summary>
    /// Gets or sets the name alternatives to look for when identifying this property.
    /// Used in navigation renaming since prediction of the reverse engineered name can be difficult.
    /// This way, for example, the user can supply options using the concatenated form like "Fk+Navigation(s)"
    /// as well as the basic pluralized form.
    /// </summary>
    [DataMember(Order = 1)]
    public HashSet<string> Name { get; set; } = new(2);

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    internal string FirstName => Name?.FirstOrDefault();

    [DataMember(Order = 2)]
    public string NewName { get; set; }

    /// <summary> The foreign key name for this relationship (if any) </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public string FkName { get; set; }

    /// <summary> The name of the inverse navigation entity </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public string ToEntity { get; set; }

    /// <summary> True if this is the principal end of the navigation.  False if this is the dependent end. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    public bool IsPrincipal { get; set; }

    /// <summary> The multiplicity of this end of the relationship. Valid values include "1", "0..1", "*" </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    public string Multiplicity { get; set; }

    IEnumerable<string> IEdmxRulePropertyModel.GetCurrentNameOptions() =>
        Name?.Where(o => o.HasNonWhiteSpace()).Distinct() ?? Array.Empty<string>();

    string IEdmxRulePropertyModel.GetNewName() => NewName;
    string IEdmxRulePropertyModel.GetNewTypeName() => null;

    public NavigationMetadata GetNavigationMetadata() =>
        new(FkName, ToEntity, IsPrincipal, Multiplicity.ParseMultiplicity());
}