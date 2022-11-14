using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules.PrimitiveNaming;

/// <summary>
/// Renaming rules for primitive properties (database columns) as well as the classes themselves (tables).
/// Navigations are not referenced in this file.
/// </summary>
[DataContract]
public sealed class PrimitiveNamingRules : IEdmxRuleModelRoot {
    [DataMember(Order = 1)]
    public bool PreserveCasingUsingRegex { get; set; }

    [DataMember(Order = 2)]
    public List<SchemaRule> Schemas { get; set; } = new();


    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.PrimitiveNaming;

    IEnumerable<IEdmxRuleClassModel> IEdmxRuleModelRoot.GetClasses() => Schemas.SelectMany(o => o.Tables);
}