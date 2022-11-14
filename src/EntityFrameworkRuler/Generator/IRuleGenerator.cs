using System.Threading.Tasks;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Rules;

// ReSharper disable MemberCanBeInternal
namespace EntityFrameworkRuler.Generator;

public interface IRuleGenerator : IRuleProcessor {
    GeneratorOptions Options { get; }

    /// <summary>
    /// Service that decides how to name navigation properties.
    /// Similar to EF ICandidateNamingService but this one utilizes the EDMX model only.
    /// </summary>
    IEdmxRulerNamingService NamingService { get; set; }

    /// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX.
    /// Errors are monitored and added to local Errors collection. </summary>
    GenerateRulesResponse TryGenerateRules();

    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="rules"> The rule models to save. </param>
    /// <param name="projectBasePath"> The location to save the rule files. </param>
    /// <param name="fileNameOptions"> Custom naming options for the rule files.  Optional. This parameter can be used to skip writing a rule file by setting that rule file to null. </param>
    /// <returns> True if completed with no errors.  When false, see Errors collection for details. </returns>
    /// <exception cref="Exception"></exception>
    Task<SaveRulesResponse> TrySaveRules(IEnumerable<IEdmxRuleModelRoot> rules, string projectBasePath,
        RuleFileNameOptions fileNameOptions = null);
}