using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Extension;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class TypeExtensions {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static Type UnwrapNullableType(this Type type)
        => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static object GetDefaultValue(this Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static bool IsNullableTypeOfAnyKind(this Type type) => !type.IsValueType || type.IsNullableGenericType();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static bool IsNullableGenericType(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static IEnumerable<Type> GetGenericInterfaces(this Type type) => type.GetInterfaces().Where(t => t.IsGenericType);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static IEnumerable<Type> GetGenericInterfaces(this Type type, Type openGeneric)
        => type.GetInterfaces().Where(q => q.IsGenericType && q.GetGenericTypeDefinition() == openGeneric);


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

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static double RangeLimit(this double value, double min, double max) {
        return Math.Max(Math.Min(value, max), min);
    }
}