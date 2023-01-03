#pragma warning disable CS8632

// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable MemberCanBeInternal
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace EntityFrameworkRuler.Common;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
internal class CSharpUniqueNamer<T> : CSharpNamer<T>
    where T : notnull {
    private readonly HashSet<string> _usedNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public CSharpUniqueNamer(
        Func<T, string> nameGetter,
        ICSharpUtilities cSharpUtilities,
        Func<string, string> singularizePluralizer)
        : this(nameGetter, null, cSharpUtilities, singularizePluralizer) {
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public CSharpUniqueNamer(Func<T, string> nameGetter, IEnumerable<string> usedNames, ICSharpUtilities cSharpUtilities,
        Func<string, string> singularizePluralizer)
        : base(nameGetter, cSharpUtilities, singularizePluralizer) {
        if (usedNames == null) return;
        foreach (var name in usedNames)
            _usedNames.Add(name);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public override string GetName(T item) {
        if (NameCache.ContainsKey(item)) return base.GetName(item);

        var input = base.GetName(item);
        var name = input;
        var suffix = 1;

        while (_usedNames.Contains(name)) name = input + suffix++;

        _usedNames.Add(name);
        NameCache[item] = name;

        return name;
    }
}