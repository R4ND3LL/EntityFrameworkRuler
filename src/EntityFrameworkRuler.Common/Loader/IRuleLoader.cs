using EntityFrameworkRuler.Common;

namespace EntityFrameworkRuler.Loader;

/// <summary> Rule loader </summary>
public interface IRuleLoader : IRuleProcessor {
    /// <summary> Options for loading rules </summary>
    LoaderOptions Options { get; }

    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="fileNameOptions"> optional rule file naming options </param>
    /// <returns> Response with loaded rules and list of errors. </returns>
    Task<LoadRulesResponse> LoadRulesInProjectPath(RuleFileNameOptions fileNameOptions = null);
}