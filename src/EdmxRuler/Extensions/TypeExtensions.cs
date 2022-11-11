using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EdmxRuler.Generator.EdmxModel;

namespace EdmxRuler.Extensions;

public static class TypeExtensions {
    public static Type UnwrapNullableType(this Type type)
        => Nullable.GetUnderlyingType(type) ?? type;

    public static Multiplicity ParseMultiplicity(this string s) {
        return s switch {
            "0..1" => Multiplicity.ZeroOne,
            "1" => Multiplicity.One,
            "*" => Multiplicity.Many,
            _ => Multiplicity.Unknown,
        };
    }

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