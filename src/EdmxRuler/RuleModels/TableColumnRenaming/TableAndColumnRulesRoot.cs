using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EdmxRuler.RuleModels.TableColumnRenaming;

[DataContract]
public sealed class TableAndColumnRulesRoot : IEdmxRuleModelRoot {
    [DataMember]
    public List<Schema> Schemas { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.TableAndColumnNaming;
}