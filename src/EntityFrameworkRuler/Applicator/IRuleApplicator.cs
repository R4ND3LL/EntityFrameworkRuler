using System.Threading.Tasks;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Applicator;

/// <summary> Apply EF model rules to existing code files using Roslyn. </summary>
public interface IRuleApplicator : IRuleLoader {
    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="projectBasePath"> The target project to apply changes to. </param>
    /// <param name="adhocOnly"> Form an adhoc in-memory project out of the target entity model files instead of loading project directly. </param>
    /// <returns> LoadAndApplyRulesResponse </returns>
    Task<LoadAndApplyRulesResponse> ApplyRulesInProjectPath(string projectBasePath, bool adhocOnly = false);

    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="request"></param>
    /// <returns> LoadAndApplyRulesResponse </returns>
    Task<LoadAndApplyRulesResponse> ApplyRulesInProjectPath(LoadAndApplyOptions request);


    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="projectBasePath"> The target project to apply changes to. </param>
    /// <param name="rules"> The rule models to apply </param>
    /// <returns> ApplyRulesResponse </returns>
    Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(string projectBasePath, params IRuleModelRoot[] rules);

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="projectBasePath"> The target project to apply changes to. </param>
    /// <param name="adhocOnly"> Form an adhoc in-memory project out of the target entity model files instead of loading project directly. </param>
    /// <param name="rules"> The rule models to apply </param>
    /// <returns> ApplyRulesResponse </returns>
    Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(string projectBasePath, bool adhocOnly, params IRuleModelRoot[] rules);

    /// <summary> Apply the given rules to the target project. </summary>
    /// <returns> ApplyRulesResponse </returns>
    Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(ApplicatorOptions request);
}