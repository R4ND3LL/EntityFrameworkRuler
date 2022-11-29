using EntityFrameworkRuler.Common;

namespace EntityFrameworkRuler.Loader;

/// <summary> Service that can load a rule model from disk </summary>
public interface IRuleLoader : IRuleHandler {
    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="projectBasePath"> The target project path containing rule files and entity models.  Or, the full json file path itself. </param>
    /// <param name="dbContextRulesFile"> The name to use for the DB context rules file.  Default is a mask that incorporates the DB context name. Optional. </param>
    /// <returns> Response with loaded rules and list of errors (if any). </returns>
    Task<LoadRulesResponse> LoadRulesInProjectPath(string projectBasePath, string dbContextRulesFile = null);

    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="request"> The load request options. </param>
    /// <returns> Response with loaded rules and list of errors (if any). </returns>
    Task<LoadRulesResponse> LoadRulesInProjectPath(ILoadOptions request);
}