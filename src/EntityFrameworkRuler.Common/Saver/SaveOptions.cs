// ReSharper disable MemberCanBeInternal

using System.Runtime.Serialization;
using EntityFrameworkRuler.Rules;
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Saver;

/// <summary> Options for loading rules </summary>
[DataContract]
public class SaveOptions : RuleFileNameOptions {
    /// <summary> Creates save options </summary>
    public SaveOptions() { }

    /// <summary> Creates save options </summary>
    public SaveOptions(string projectBasePath) : this() { ProjectBasePath = projectBasePath; }

    /// <summary> Creates save options </summary>
    public SaveOptions(IRuleModelRoot rule, string projectBasePath, string dbContextRulesFile = null)
        : this(rule != null ? new[] { rule } : ArraySegment<IRuleModelRoot>.Empty, projectBasePath, dbContextRulesFile) {
    }

    /// <summary> Creates save options </summary>
    public SaveOptions(string projectBasePath, params IRuleModelRoot[] rules)
        : this(rules, projectBasePath, null) {
    }

    /// <summary> Creates save options </summary>
    public SaveOptions(string projectBasePath, string dbContextRulesFile, params IRuleModelRoot[] rules)
        : this(rules, projectBasePath, dbContextRulesFile) {
    }

    /// <summary> Creates save options </summary>
    public SaveOptions(IEnumerable<IRuleModelRoot> rules, string projectBasePath, string dbContextRulesFile) {
        if (rules != null) Rules.AddRange(rules);
        ProjectBasePath = projectBasePath;
        if (dbContextRulesFile.HasNonWhiteSpace()) DbContextRulesFile = dbContextRulesFile;
    }


    /// <summary> The target project path containing rule files and entity models. </summary>
    [DataMember]
    public string ProjectBasePath { get; set; }

    /// <summary> The rules to save. </summary>
    [DataMember]
    public IList<IRuleModelRoot> Rules { get; set; } = new List<IRuleModelRoot>();
}