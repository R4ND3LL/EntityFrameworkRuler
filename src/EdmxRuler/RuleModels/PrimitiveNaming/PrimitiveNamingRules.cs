using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;

[DataContract]
public sealed class PrimitiveNamingRules : IEdmxRuleModelRoot {
    [DataMember]
    public List<Schema> Schemas { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.TableAndColumnNaming;
}