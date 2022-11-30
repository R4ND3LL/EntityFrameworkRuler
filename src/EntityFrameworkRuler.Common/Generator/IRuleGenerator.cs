using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Saver;

// ReSharper disable MemberCanBeInternal
namespace EntityFrameworkRuler.Generator;

/// <summary> Rule generator </summary>
public interface IRuleGenerator : IRuleSaver {
    /// <summary>
    /// Service that decides how to name navigation properties.
    /// Similar to EF ICandidateNamingService but this one utilizes the EDMX model only.
    /// </summary>
    IRulerNamingService NamingService { get; set; }

    /// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX.
    /// Errors are monitored and added to local Errors collection. </summary>
    /// <param name="request"> The generation request options. </param>
    Task<GenerateRulesResponse> GenerateRulesAsync(GeneratorOptions request);

    /// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX.
    /// Errors are monitored and added to local Errors collection. </summary>
    /// <param name="edmxFilePath"> The EDMX file to generate rule from </param>
    /// <param name="useDatabaseNames"> Gets or sets a value indicating whether the expected Reverse Engineered entities
    /// will use the database names directly. </param>
    /// <param name="noPluralize"> If true then disable pluralization of expected Reverse Engineered entity elements. </param>
    /// <param name="includeUnknowns"> Set rules to include unknowns schemas, tables, and columns.  That is, allow items to be scaffolded that are not
    /// identified in the rules.  Default is false. </param>
    /// <param name="compactRules"> Include only rules for things that differ from their default Reverse Engineered state.
    /// Default is false.  Only respected when IncludeUnknowns is true.
    /// If IncludeUnknowns is false, then all rules must appear in the output in order to identify what should be excluded. </param>
    GenerateRulesResponse GenerateRules(string edmxFilePath, bool useDatabaseNames = false, bool noPluralize = false,
        bool includeUnknowns = false, bool compactRules = false);

    /// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX.
    /// Errors are monitored and added to local Errors collection. </summary>
    /// <param name="request"> The generation request options. </param>
    GenerateRulesResponse GenerateRules(GeneratorOptions request);
}