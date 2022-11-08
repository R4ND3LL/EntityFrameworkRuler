using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EdmxRuler.RuleModels.PrimitiveNaming;

/// <summary>
/// Renaming rules for primitive properties (database columns) as well as the classes themselves (tables).
/// Navigations are not referenced in this file.
/// </summary>
[DataContract]
public sealed class PrimitiveNamingRules : IEdmxRuleModelRoot {
    [DataMember]
    public List<Schema> Schemas { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.TableAndColumnNaming;
}