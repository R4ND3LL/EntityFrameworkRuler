﻿using System.Runtime.Serialization;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Applicator;

[DataContract]
public class ApplicatorOptions {
    public ApplicatorOptions() { }
    public ApplicatorOptions(string projectBasePath, params IRuleModelRoot[] rules) : this(projectBasePath, false, rules) { }

    public ApplicatorOptions(string projectBasePath, bool adhocOnly, params IRuleModelRoot[] rules) {
        ProjectBasePath = projectBasePath;
        AdhocOnly = adhocOnly;
        Rules = rules;
    }

    /// <summary> The rule models to apply </summary>
    [DataMember]
    public IList<IRuleModelRoot> Rules { get; set; } = new List<IRuleModelRoot>();

    /// <summary> The target project to apply changes to. </summary>
    [DataMember]
    public string ProjectBasePath { get; set; }

    /// <summary> Form an adhoc in-memory project out of the target entity model files instead of loading project directly. </summary>
    [DataMember]
    public bool AdhocOnly { get; set; }

    /// <summary> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded during adhoc process. </summary>
    [DataMember]
    public string ContextFolder { get; set; }

    /// <summary> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded during adhoc process. </summary>
    [DataMember]
    public string ModelsFolder { get; set; }
}

[DataContract]
public sealed class LoadAndApplyOptions : ApplicatorOptions, ILoadOptions {
    public LoadAndApplyOptions() => DbContextRulesFile = RuleFileNameOptions.DefaultDbContextRulesFile;

    /// <summary> The name to use for the DB context rules file.  Default is a mask that incorporates the DB context name. </summary>
    [DataMember]
    public string DbContextRulesFile { get; set; }
}