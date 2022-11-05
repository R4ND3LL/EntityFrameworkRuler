using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EdmxRuler.RuleModels.TableColumnRenaming;

namespace EdmxRuler.RuleModels.EnumMapping;

[DataContract]
public sealed class EnumMappingRulesRoot : IEdmxRuleModelRoot {
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string Namespace { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<EnumMappingClass> Classes { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.EnumMapping;
}