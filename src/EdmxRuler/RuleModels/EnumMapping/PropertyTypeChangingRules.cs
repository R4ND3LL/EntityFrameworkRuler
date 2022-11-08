using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EdmxRuler.RuleModels.EnumMapping;

/// <summary> Property type changing rules. </summary>
[DataContract]
public sealed class PropertyTypeChangingRules : IEdmxRuleModelRoot {
    /// <summary> Optional namespace used when identifying classes.  Setting this will help to positively identify ambiguously names classes. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string Namespace { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<TypeChangingClass> Classes { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.PropertyTypeMapping;

    IEnumerable<IEdmxRuleClassModel> IEdmxRuleModelRoot.GetClasses() => Classes;
}