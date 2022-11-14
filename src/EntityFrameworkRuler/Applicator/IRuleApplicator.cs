using System.Threading.Tasks;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Rules.NavigationNaming;
using EntityFrameworkRuler.Rules.PrimitiveNaming;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Applicator;

public interface IRuleApplicator : IRuleLoader {
    new ApplicatorOptions Options { get; }

    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="fileNameOptions"> optional rule file naming options </param>
    /// <returns> List of errors. </returns>
    Task<LoadAndApplyRulesResponse> ApplyRulesInProjectPath(RuleFileNameOptions fileNameOptions = null);

    /// <summary> Apply the given rules to the target project. </summary>
    /// <returns> List of errors. </returns>
    Task<IReadOnlyList<ApplyRulesResponse>> ApplyRules(IEnumerable<IEdmxRuleModelRoot> rules);

    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="navigationNamingRules"> The rules to apply. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    Task<ApplyRulesResponse> ApplyRules(NavigationNamingRules navigationNamingRules, string contextFolder = null,
        string modelsFolder = null);


    /// <summary> Apply the given rules to the target project. </summary>
    /// <param name="primitiveNamingRules"> The rules to apply. </param>
    /// <param name="contextFolder"> Optional folder where data context is found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <param name="modelsFolder"> Optional folder where models are found. If provided, only cs files in the target subfolders will be loaded. </param>
    /// <returns></returns>
    Task<ApplyRulesResponse> ApplyRules(PrimitiveNamingRules primitiveNamingRules, string contextFolder = null, string modelsFolder = null);
}