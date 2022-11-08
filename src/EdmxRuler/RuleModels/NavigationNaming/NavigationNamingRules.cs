using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EdmxRuler.RuleModels.NavigationNaming;

/// <summary> Navigation property naming rules </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class NavigationNamingRules : IEdmxRuleModelRoot {
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string Namespace { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<ClassReference> Classes { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.ClassAndNavigationNaming;
}