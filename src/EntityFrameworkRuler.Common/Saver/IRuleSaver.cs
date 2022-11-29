using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Saver;

/// <summary> Service that can persist a rule model to disk </summary>
public interface IRuleSaver : IRuleHandler {
    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="projectBasePath"> The location to save the rule files. </param>
    /// <param name="dbContextRulesFile"> The name to use for the DB context rules file.  Default is a mask that incorporates the DB context name. Optional. </param>
    /// <param name="rules"> The rule models to save. </param>
    /// <returns> Save Rules Response. </returns>
    Task<SaveRulesResponse> SaveRules(string projectBasePath, string dbContextRulesFile = null, params IRuleModelRoot[] rules);

    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="request"> The save request options. </param>
    /// <exception cref="Exception"></exception>
    Task<SaveRulesResponse> SaveRules(SaveOptions request);
}