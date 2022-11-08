﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EdmxRuler.RuleModels.EnumMapping;

/// <summary> Property enum type mapping rules. </summary>
[DataContract]
public sealed class EnumMappingRules : IEdmxRuleModelRoot {
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string Namespace { get; set; }

    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public List<EnumMappingClass> Classes { get; set; } = new();

    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public EdmxRuleModelKind Kind => EdmxRuleModelKind.EnumMapping;
}