using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary>
/// Renaming rules for primitive properties (database columns) as well as the classes themselves (tables).
/// Navigations are not referenced in this file.
/// </summary>
[DataContract]
public sealed class DbContextRule : IRuleModelRoot {
    internal static DbContextRule DefaultNoRulesFoundBehavior => new() { IncludeUnknownSchemas = true };

    /// <summary> DB context name </summary>
    [DataMember(Order = 1)]
    public string Name { get; set; }

    /// <summary> Preserve casing using regex </summary>
    [DataMember(Order = 2)]
    public bool PreserveCasingUsingRegex { get; set; }

    /// <summary> If true, generate entity models for schemas that are not identified in this ruleset.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 3)]
    public bool IncludeUnknownSchemas { get; set; }

    /// <summary> If true, EntityTypeConfigurations will be split into separate files using EntityTypeConfiguration.t4 for EF >= 7.  Default is false. </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = false, Order = 4)]
    public bool SplitEntityTypeConfigurations { get; set; }

    /// <summary> Schema rules </summary>
    [DataMember(Order = 100)]
    public List<SchemaRule> Schemas { get; set; } = new();


    /// <inheritdoc />
    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public RuleModelKind Kind => RuleModelKind.DbContext;

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    internal string FilePath { get; set; }

    string IRuleModelRoot.GetName() => Name.NullIfEmpty();
    IEnumerable<IClassRule> IRuleModelRoot.GetClasses() => Schemas.SelectMany(o => o.Tables);
}