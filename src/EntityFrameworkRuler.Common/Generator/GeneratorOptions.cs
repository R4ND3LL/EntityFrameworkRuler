// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Generator;

/// <summary> Generator options </summary>
public sealed class GeneratorOptions : IPluralizerOptions {
    /// <summary> Creates Generator options </summary>
    public GeneratorOptions() : this(null) { }

    /// <summary> Creates Generator options </summary>
    public GeneratorOptions(string edmxFilePath, bool useDatabaseNames = false, bool noPluralize = false, bool includeUnknownSchemasAndTables = false,
        bool includeUnknownColumns = true) {
        EdmxFilePath = edmxFilePath;
        UseDatabaseNames = useDatabaseNames;
        NoPluralize = noPluralize;
        IncludeUnknownSchemasAndTables = includeUnknownSchemasAndTables;
        IncludeUnknownColumns = includeUnknownColumns;
    }

    /// <summary> The EDMX file to generate rule from </summary>
    public string EdmxFilePath { get; set; }

    /// <summary>  Gets or sets a value indicating whether the expected Reverse Engineered entities
    /// will use the database names directly. </summary>
    public bool UseDatabaseNames { get; set; }

    /// <summary> If true then disable pluralization of expected Reverse Engineered entity elements. </summary>
    public bool NoPluralize { get; set; }

    /// <summary> Set rules to include unknowns schemas, and tables.  That is, allow items to be scaffolded that are not
    /// identified in the rules.  Default is false. </summary>
    public bool IncludeUnknownSchemasAndTables { get; set; }

    /// <summary> Set rules to include unknowns columns.  That is, allow items to be scaffolded that are not
    /// identified in the rules.  Default is true. </summary>
    public bool IncludeUnknownColumns { get; set; }
}

/// <summary> Pluralizer options </summary>
public interface IPluralizerOptions {
    /// <summary>  Gets or sets a value indicating whether the expected Reverse Engineered entities
    /// will use the database names directly. </summary>
    bool UseDatabaseNames { get; set; }

    /// <summary> If true then disable pluralization of expected Reverse Engineered entity elements. </summary>
    bool NoPluralize { get; set; }
}