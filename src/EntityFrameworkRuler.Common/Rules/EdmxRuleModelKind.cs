namespace EntityFrameworkRuler.Rules;

/// <summary> Kind of rule file </summary>
public enum EdmxRuleModelKind : byte {
    /// <summary> Primitive rules </summary>
    PrimitiveNaming = 1,

    /// <summary> Navigation rules </summary>
    NavigationNaming = 2,
}