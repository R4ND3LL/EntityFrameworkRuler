// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Generator;

public sealed class GeneratorOptions {
    public string EdmxFilePath { get; set; }
    public string ProjectBasePath { get; set; }

    /// <summary>
    /// If true, generate rule files with no extra metadata about the entity models.  Only generate minimal change information.
    /// </summary>
    public bool NoMetadata { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to use the database schema names directly.
    /// </summary>
    /// <value> A value indicating whether to use the database schema names directly. </value>
    public bool UseDatabaseNames { get; set; }

    /// <summary> If true then disable pluralization of entity elements. </summary>
    public bool NoPluralize { get; set; }

    /// <summary> Set rules to include unknowns schemas, tables, and columns.  That is, allow items to be scaffolded that are not
    /// identified in the rules.  Default is false. </summary>
    public bool IncludeUnknowns { get; set; }

    /// <summary>
    /// Include only rules for things that differ from their default Reverse Engineered state.
    /// Default is false.  Only used when IncludeUnknowns is true.
    /// If IncludeUnknowns is false, then all rules must appear in the output in order to identify what should be excluded. </summary>
    public bool CompactRules { get; set; }
}