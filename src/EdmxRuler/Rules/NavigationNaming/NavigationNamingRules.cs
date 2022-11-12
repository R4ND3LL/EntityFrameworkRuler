using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EdmxRuler.Rules.NavigationNaming;

/// <summary> Navigation property naming rules </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class NavigationNamingRules : IEdmxRuleModelRoot {
    /// <summary> Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously names classes. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false)]
    public string Namespace { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<ClassReference> Classes { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.NavigationNaming;
    IEnumerable<IEdmxRuleClassModel> IEdmxRuleModelRoot. GetClasses() => Classes;
}