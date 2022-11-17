using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Extension;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class TypeExtensions {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static Type UnwrapNullableType(this Type type)
        => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static Multiplicity ParseMultiplicity(this string s) {
        return s switch {
            "0..1" => Multiplicity.ZeroOne,
            "1" => Multiplicity.One,
            "*" => Multiplicity.Many,
            _ => Multiplicity.Unknown,
        };
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string ToMultiplicityString(this Multiplicity m) {
        return m switch {
            Multiplicity.Unknown => string.Empty,
            Multiplicity.ZeroOne => "0..1",
            Multiplicity.One => "1",
            Multiplicity.Many => "*",
            _ => throw new ArgumentOutOfRangeException(nameof(m), m, null)
        };
    }
}