using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Navigation rule </summary>
[DebuggerDisplay("Nav {Name} to {NewName} FkName={FkName} IsPrincipal={IsPrincipal}")]
[DataContract]
public sealed class NavigationRule : RuleBase, IPropertyRule {
    /// <summary> Creates a navigation rule </summary>
    public NavigationRule() { }

    /// <summary>
    /// Gets or sets the expected EF generated name given to this navigation property.
    /// Used in navigation renaming via Roslyn where the existing code property has to be located and renamed.
    /// </summary>
    [DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
    [DisplayName("Expected Name"), Category("Mapping"),
     Description("The expected EF generated name for the navigation property.")]
    [JsonConverter(typeof(NavigationRuleNameConverter))]
    public string Name { get; set; }

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
    [DisplayName("Is Principal End"), Category("Mapping"),
     Description("True if, this is the principal end of the navigation.  False if this is the dependent end.")]
    public bool IsPrincipal { get; set; }

    /// <summary> The multiplicity of this end of the relationship. Valid values include "1", "0..1", "*" </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 6)]
    [DisplayName("Multiplicity"), Category("Mapping"),
     Description("The multiplicity of this end of the relationship. Valid values include \"1\", \"0..1\", \"*\"")]
    public string Multiplicity { get; set; }

    /// <inheritdoc />
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 7)]
    [DisplayName("Not Mapped"), Category("Mapping"), Description("If true, omit this table during the scaffolding process.")]
    public override bool NotMapped { get; set; }

    /// <inheritdoc />
    protected override string GetDbName() => Name;

    /// <inheritdoc />
    protected override string GetNewName() => NewName.NullIfEmpty();

    /// <inheritdoc />
    protected override string GetExpectedEntityFrameworkName() => Name.NullIfWhitespace();

    /// <inheritdoc />
    protected override void SetFinalName(string value) {
        NewName = value;
        //OnPropertyChanged(nameof(NewName));
    }

    IEnumerable<string> IPropertyRule.GetCurrentNameOptions() => new[] { Name };

    string IPropertyRule.GetNewTypeName() => null;

    /// <inheritdoc />
    public NavigationMetadata GetNavigationMetadata() =>
        new(FkName, ToEntity, IsPrincipal, Multiplicity.ParseMultiplicity());
}

public class NavigationRuleNameConverter : JsonConverter<string> {
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        string result = null;
        if (reader.TokenType == JsonTokenType.StartArray)
            while (true) {
                if (!reader.Read()) throw new JsonException($"Bad JSON");

                if (reader.TokenType == JsonTokenType.EndArray) break;

                if (result != null) continue;
                var s = reader.GetString();
                if (s.HasNonWhiteSpace()) result = s;
            }

        if (reader.TokenType == JsonTokenType.String) {
            var s = reader.GetString();
            return s;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) {
        writer.WriteStringValue((string)value);
    }
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