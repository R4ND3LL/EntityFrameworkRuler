using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Foreign Key rule.  Typically defined in json only for conceptual navigations as a means to identify the column mapping,
/// which would not be available from the database during scaffolding. </summary>
[DebuggerDisplay("FK {Name}")]
[DataContract]
public sealed class ForeignKeyRule : RuleBase, ISchemaRule {
    /// <summary> Constraint name </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = true, Order = 1)]
    [DisplayName("Name"), Category("Mapping"), Description("The constraint name this FK rule applies to.  Required."), Required]
    public string Name { get; set; }

    /// <summary> Principal entity name </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = true, Order = 2)]
    [DisplayName("Principal Entity"), Category("Mapping"), Description("Principal entity name.  Required."), Required]
    public string PrincipalEntity { get; set; }

    /// <summary> Principal entity properties </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = true, Order = 3)]
    [DisplayName("Principal Properties"), Category("Mapping"), Description("Principal entity properties.  Required."), Required]
    public string[] PrincipalProperties { get; set; }

    /// <summary> Dependent entity name </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = true, Order = 4)]
    [DisplayName("Dependent Entity"), Category("Mapping"), Description("Dependent entity name.  Required."), Required]
    public string DependentEntity { get; set; }

    /// <summary> Dependent entity properties </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = true, Order = 5)]
    [DisplayName("Dependent Properties"), Category("Mapping"), Description("Dependent entity properties.  Required."), Required]
    public string[] DependentProperties { get; set; }

    /// <inheritdoc />
    protected override string GetNewName() => null;

    /// <inheritdoc />
    protected override void SetFinalName(string value) { Name = value; }

    /// <inheritdoc />
    protected override bool GetNotMapped() => false;

    /// <inheritdoc />
    protected override string GetDbName() => Name;

    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => Name;

    IEnumerable<IEntityRule> ISchemaRule.GetClasses() => Array.Empty<IEntityRule>();
}