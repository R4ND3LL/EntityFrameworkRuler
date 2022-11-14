using System.Runtime.Serialization;

namespace EntityFrameworkRuler;

/// <summary> Options to allow caller to customize the default rule file names for processes that read or write the rule files directly. </summary>
[DataContract]
public sealed class RuleFileNameOptions {
    /// <summary> table and column renaming file. i.e. primitive properties only </summary>
    [DataMember]
    public string PrimitiveRulesFile { get; set; } = "primitive-rules.json";

    /// <summary> property renaming rules file (for renaming navigations) </summary>
    [DataMember]
    public string NavigationRulesFile { get; set; } = "navigation-rules.json";

}