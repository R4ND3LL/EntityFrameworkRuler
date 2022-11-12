using System.Runtime.Serialization;

namespace EdmxRuler;

/// <summary> Options to allow caller to customize the default rule file names for processes that read or write the rule files directly. </summary>
[DataContract]
public sealed class RuleFileNameOptions {
    /// <summary> table and column renaming file. i.e. primitive properties only </summary>
    [DataMember]
    public string PrimitiveNamingFile { get; set; } = "primitive-naming.json";

    /// <summary> property renaming rules file (for renaming navigations) </summary>
    [DataMember]
    public string NavigationNamingFile { get; set; } = "navigation-naming.json";

    /// <summary> property type mapping rule file. </summary>
    [DataMember]
    public string PropertyTypeChangingFile { get; set; } = "property-types.json";
}