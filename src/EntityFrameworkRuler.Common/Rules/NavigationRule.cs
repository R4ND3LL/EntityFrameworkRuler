using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <inheritdoc />
[DebuggerDisplay("Nav {FirstName} to {NewName} FkName={FkName} IsPrincipal={IsPrincipal}")]
[DataContract]
public sealed class NavigationRule : RuleBase, IPropertyRule {
    /// <summary>
    /// Gets or sets the name alternatives to look for when identifying this property.
    /// Used in navigation renaming since prediction of the reverse engineered name can be difficult.
    /// This way, for example, the user can supply options using the concatenated form like "Fk+Navigation(s)"
    /// as well as the basic pluralized form.
    /// </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("Expected Name(s)"), Category("Mapping"), Description("Gets or sets the name alternatives to look for when identifying this property.")]
    public List<string> Name { get; set; } = new(2);

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    internal string FirstName => Name?.FirstOrDefault();

    /// <summary> New name of property. Optional. </summary>
    [DataMember(Order = 2)]
    [DisplayName("New Name"), Category("Mapping"), Description("New name of property. Optional.")]
    public string NewName { get; set; }

    /// <summary> The foreign key name for this relationship (if any). </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
    [DisplayName("Constraint Name"), Category("Mapping"), Description("The foreign key name for this relationship (if any).")]
    public string FkName { get; set; }

    /// <summary> The name of the inverse navigation entity. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 4)]
    [DisplayName("To Entity"), Category("Mapping"), Description("The name of the inverse navigation entity.")]
    public string ToEntity { get; set; }

    /// <summary> True if, this is the principal end of the navigation.  False if this is the dependent end. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 5)]
    [DisplayName("Is Principal End"), Category("Mapping"), Description("True if, this is the principal end of the navigation.  False if this is the dependent end.")]
    public bool IsPrincipal { get; set; }

    /// <summary> The multiplicity of this end of the relationship. Valid values include "1", "0..1", "*" </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    [DisplayName("Multiplicity"), Category("Mapping"), Description("The multiplicity of this end of the relationship. Valid values include \"1\", \"0..1\", \"*\"")]
    public string Multiplicity { get; set; }

    /// <inheritdoc />
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this table during the scaffolding process.")]
    public override bool NotMapped { get; set; }

    /// <inheritdoc />
    protected override string GetNewName() => NewName.NullIfEmpty();
    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => Name?.FirstOrDefault(o => o.HasNonWhiteSpace());
    /// <inheritdoc />
    protected override void SetFinalName(string value) {
        NewName = value;
        OnPropertyChanged(nameof(NewName));
    }

    IEnumerable<string> IPropertyRule.GetCurrentNameOptions() =>
        Name?.Where(o => o.HasNonWhiteSpace()).Distinct() ?? Array.Empty<string>();

    string IPropertyRule.GetNewTypeName() => null;

    /// <inheritdoc />
    public NavigationMetadata GetNavigationMetadata() =>
        new(FkName, ToEntity, IsPrincipal, Multiplicity.ParseMultiplicity());
}

/// <summary> Multiplicity, or expected count, on a navigation end. </summary>
public enum Multiplicity {
    /// <summary> Unknown </summary>
    Unknown = 0,
    /// <summary> ZeroOne </summary>
    ZeroOne,
    /// <summary> One </summary>
    One,
    /// <summary> Many </summary>
    Many
}