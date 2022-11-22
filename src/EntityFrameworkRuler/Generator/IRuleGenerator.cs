using System.Threading.Tasks;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Saver;

// ReSharper disable MemberCanBeInternal
namespace EntityFrameworkRuler.Generator;

public interface IRuleGenerator : IRuleSaver {
    new GeneratorOptions Options { get; }

    /// <summary>
    /// Service that decides how to name navigation properties.
    /// Similar to EF ICandidateNamingService but this one utilizes the EDMX model only.
    /// </summary>
    IRulerNamingService NamingService { get; set; }

    /// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX.
    /// Errors are monitored and added to local Errors collection. </summary>
    GenerateRulesResponse TryGenerateRules();

    
}