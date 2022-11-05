using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EdmxRuler.RuleModels.TableColumnRenaming;

namespace EdmxRuler.RuleModels.PropertyRenaming;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:", Justification = "Reviewed.")]
[DataContract]
public sealed class ClassPropertyNamingRulesRoot : IEdmxRuleModelRoot {
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string Namespace { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<ClassRenamer> Classes { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.ClassAndNavigationNaming;
}