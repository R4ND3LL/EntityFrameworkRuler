using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules.NavigationNaming;

/// <summary> Navigation property naming rules </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class NavigationNamingRules : IRuleModelRoot {
    /// <summary> Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously names classes. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 1)]
    public string Namespace { get; set; }

    /// <summary> Class rules </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public List<ClassReference> Classes { get; set; } = new();

    /// <inheritdoc />
    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public RuleModelKind Kind => RuleModelKind.NavigationNaming;

    IEnumerable<IClassRule> IRuleModelRoot.GetClasses() => Classes;
}