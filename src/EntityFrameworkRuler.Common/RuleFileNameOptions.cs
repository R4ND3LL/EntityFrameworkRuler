using System.Runtime.Serialization;

namespace EntityFrameworkRuler;

/// <summary> Options to allow caller to customize the default rule file names for processes that read or write the rule files directly. </summary>
[DataContract]
public abstract class RuleFileNameOptions {
    public static string DefaultDbContextRulesFile = "<ContextName>-rules.json";

    public RuleFileNameOptions() { DbContextRulesFile = DefaultDbContextRulesFile; }

    /// <summary> The name to use for the DB context rules file.  Default is a mask that incorporates the DB context name. </summary>
    [DataMember]
    public string DbContextRulesFile { get; set; }
}