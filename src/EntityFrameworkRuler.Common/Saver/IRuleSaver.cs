using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Saver;

/// <summary> Rule Saver </summary>
public interface IRuleSaver : IRuleProcessor {
    /// <summary> Options for loading rules </summary>
    SaveOptions Options { get; }

    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="rule"> The rule model to save. </param>
    /// <param name="projectBasePath"> The location to save the rule files. </param>
    /// <param name="fileNameOptions"> Custom naming options for the rule files.  Optional. This parameter can be used to skip writing a rule file by setting that rule file to null. </param>
    /// <returns> True if completed with no errors.  When false, see Errors collection for details. </returns>
    /// <exception cref="Exception"></exception>
    public Task<SaveRulesResponse> TrySaveRules(IRuleModelRoot rule, string projectBasePath,
        RuleFileNameOptions fileNameOptions = null) => TrySaveRules(new[] { rule }, projectBasePath, fileNameOptions);

    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="rules"> The rule models to save. </param>
    /// <param name="projectBasePath"> The location to save the rule files. </param>
    /// <param name="fileNameOptions"> Custom naming options for the rule files.  Optional. This parameter can be used to skip writing a rule file by setting that rule file to null. </param>
    /// <returns> True if completed with no errors.  When false, see Errors collection for details. </returns>
    /// <exception cref="Exception"></exception>
    Task<SaveRulesResponse> TrySaveRules(IEnumerable<IRuleModelRoot> rules, string projectBasePath,
        RuleFileNameOptions fileNameOptions = null);
}