// ReSharper disable MemberCanBeInternal

using System.Runtime.Serialization;

// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Loader;

/// <summary> Options for loading rules </summary>
public interface ILoadOptions {
    /// <summary> The target project path containing rule files and entity models. Or, the exact rule file path to load. </summary>
    string ProjectBasePath { get; set; }

    /// <summary> The name to use for the DB context rules file.  Default is a mask that incorporates the DB context name. </summary>
    string DbContextRulesFile { get; set; }
}

/// <summary> Options for loading rules </summary>
[DataContract]
public class LoadOptions : RuleFileNameOptions, ILoadOptions {
    /// <summary> Creates load options </summary>
    public LoadOptions() { }

    /// <summary> Creates load options </summary>
    public LoadOptions(string projectBasePath, string dbContextRulesFile = null) {
        ProjectBasePath = projectBasePath;
        if (dbContextRulesFile.HasNonWhiteSpace()) DbContextRulesFile = dbContextRulesFile;
    }

    /// <summary> The target project path containing rule files and entity models. Or, the exact rule file path to load. </summary>
    [DataMember]
    public string ProjectBasePath { get; set; }
}