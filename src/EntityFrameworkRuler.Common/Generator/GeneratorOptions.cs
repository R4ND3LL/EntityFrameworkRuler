// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Generator;

/// <summary> Generator options </summary>
public sealed class GeneratorOptions : IPluralizerOptions {
    /// <summary> Creates Generator options </summary>
    public GeneratorOptions() { }

    /// <summary> Creates Generator options </summary>
    public GeneratorOptions(string edmxFilePath, bool useDatabaseNames = false, bool noPluralize = false, bool includeUnknowns = false,
        bool compactRules = false) {
        EdmxFilePath = edmxFilePath;
        UseDatabaseNames = useDatabaseNames;
        NoPluralize = noPluralize;
        IncludeUnknowns = includeUnknowns;
        CompactRules = compactRules;
    }

    /// <summary> The EDMX file to generate rule from </summary>
    public string EdmxFilePath { get; set; }

    /// <summary>  Gets or sets a value indicating whether the expected Reverse Engineered entities
    /// will use the database names directly. </summary>
    public bool UseDatabaseNames { get; set; }

    /// <summary> If true then disable pluralization of expected Reverse Engineered entity elements. </summary>
    public bool NoPluralize { get; set; }

    /// <summary> Set rules to include unknowns schemas, tables, and columns.  That is, allow items to be scaffolded that are not
    /// identified in the rules.  Default is false. </summary>
    public bool IncludeUnknowns { get; set; }

    /// <summary>
    /// Include only rules for things that differ from their default Reverse Engineered state.
    /// Default is false.  Only respected when IncludeUnknowns is true.
    /// If IncludeUnknowns is false, then all rules must appear in the output in order to identify what should be excluded. </summary>
    public bool CompactRules { get; set; }
}

/// <summary> Pluralizer options </summary>
public interface IPluralizerOptions {
    /// <summary>  Gets or sets a value indicating whether the expected Reverse Engineered entities
    /// will use the database names directly. </summary>
    bool UseDatabaseNames { get; set; }

    /// <summary> If true then disable pluralization of expected Reverse Engineered entity elements. </summary>
    bool NoPluralize { get; set; }
}