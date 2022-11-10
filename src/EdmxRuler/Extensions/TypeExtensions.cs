using System;
using System.Collections.Generic;
using System.Text;

namespace EdmxRuler.Extensions;

/// <summary>
///     <para>
///         Extension methods for <see cref="Type" /> instances.
///     </para>
///     <para>
///         These extensions are typically used by database providers (and other extensions). They are generally
///         not used in application code.
///     </para>
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-providers">Implementation of database providers and extensions</see>
///     for more information and examples.
/// </remarks>
public static class TypeExtensions { 

    public static Type UnwrapNullableType(this Type type)
        => Nullable.GetUnderlyingType(type) ?? type;
}