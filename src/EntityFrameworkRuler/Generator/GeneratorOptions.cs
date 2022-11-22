// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Generator;

public sealed class GeneratorOptions {
    public string EdmxFilePath { get; set; }
    public string ProjectBasePath { get; set; }

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