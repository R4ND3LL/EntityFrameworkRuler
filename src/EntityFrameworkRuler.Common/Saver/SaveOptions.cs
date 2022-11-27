// ReSharper disable MemberCanBeInternal

using System.Runtime.Serialization;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Saver;

/// <summary> Options for loading rules </summary>
[DataContract]
public class SaveOptions : RuleFileNameOptions {
    /// <summary> Creates save options </summary>
    public SaveOptions() { }

    /// <summary> Creates save options </summary>
    public SaveOptions(string projectBasePath) : this() { ProjectBasePath = projectBasePath; }

    /// <summary> Creates save options </summary>
    public SaveOptions(string projectBasePath, params IRuleModelRoot[] rules) : this(projectBasePath, null, rules) { }

    /// <summary> Creates save options </summary>
    public SaveOptions(string projectBasePath, string dbContextRulesFile, params IRuleModelRoot[] rules) {
        Rules = rules;
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