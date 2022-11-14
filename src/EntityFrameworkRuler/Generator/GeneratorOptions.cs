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
}