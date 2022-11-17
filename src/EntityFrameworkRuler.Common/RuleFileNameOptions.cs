using System.Runtime.Serialization;

namespace EntityFrameworkRuler;

/// <summary> Options to allow caller to customize the default rule file names for processes that read or write the rule files directly. </summary>
[DataContract]
public sealed class RuleFileNameOptions {
    /// <summary> The name to use for the DB context rules file. </summary>
    [DataMember]
    public string DbContextRulesFile { get; set; } = "<ContextName>-rules.json";
}