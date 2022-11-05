using System.Runtime.Serialization;

namespace EdmxRuler;

[DataContract]
public class RuleFileNameOptions {
    /// <summary> table and column renaming file. </summary>
    [DataMember]
    public string RenamingFilename { get; set; } = "primitive-renaming.json";

    /// <summary> property renaming rules file (for renaming navigations) </summary>
    [DataMember]
    public string PropertyFilename { get; set; } = "property-renaming.json";

    /// <summary> enum mapping rule file. </summary>
    [DataMember]
    public string EnumMappingFilename { get; set; } = "enum-mapping.json";
}