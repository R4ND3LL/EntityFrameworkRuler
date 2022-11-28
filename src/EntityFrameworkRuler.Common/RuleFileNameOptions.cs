using System.Runtime.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler;

/// <summary> Options to allow caller to customize the default rule file names for processes that read or write the rule files directly. </summary>
[DataContract]
public abstract class RuleFileNameOptions {
    /// <summary> The default DB context rules file name or mask </summary>
    public static string DefaultDbContextRulesFile { get; set; } = "<ContextName>-rules.json";

    /// <summary> Creates a RuleFileNameOptions </summary>
    protected RuleFileNameOptions() => DbContextRulesFile = DefaultDbContextRulesFile;

    /// <summary> The name to use for the DB context rules file.  Default is a mask that incorporates the DB context name. </summary>
    [DataMember]
    public string DbContextRulesFile { get; set; }
}