using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Design.Services;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public class RuledCSharpUniqueNamer<T> : CSharpNamer<T> {
    private readonly Func<T, (string, bool)> nameGetter;
    private readonly ICSharpUtilities cSharpUtilities;
    private readonly Func<string, string> singularizePluralizer;
    private readonly HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledCSharpUniqueNamer(Func<T, (string, bool)> nameGetter, ICSharpUtilities cSharpUtilities,
        Func<string, string> singularizePluralizer)
        : this(nameGetter, null, cSharpUtilities, singularizePluralizer) {
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledCSharpUniqueNamer(Func<T, (string, bool)> nameGetter, IEnumerable<string> usedNames, ICSharpUtilities cSharpUtilities,
        Func<string, string> singularizePluralizer)
        : base(t => throw new NotSupportedException("Base class cannot invoke the nameGetter"),
            cSharpUtilities,
            singularizePluralizer) {
        this.nameGetter = nameGetter;
        this.cSharpUtilities = cSharpUtilities;
        this.singularizePluralizer = singularizePluralizer;
        if (usedNames == null) return;
        foreach (var name in usedNames) this.usedNames.Add(name);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public override string GetName(T item) {
        if (NameCache.ContainsKey(item)) return GetNameCore(item);

        var input = GetNameCore(item);
        var name = input;
        var suffix = 1;

        while (usedNames.Contains(name)) name = input + suffix++;

        usedNames.Add(name);
        NameCache[item] = name;

        return name;
    }

    private string GetNameCore(T item) {
        if (NameCache.TryGetValue(item, out var cachedName)) return cachedName;

        var (newName, isFrozen) = nameGetter(item);

        // if allowed to pluralize/singularize name, i.e. it was not explicitly set by rule, then pass the
        // singularizePluralizer. otherwise pass null for the singularizePluralizer.
        // If this is not done, the explicitly defined names may be altered by the singularizePluralizer.
        var sp = isFrozen ? null : singularizePluralizer;

        var name = cSharpUtilities.GenerateCSharpIdentifier(newName,
            existingIdentifiers: null,
            singularizePluralizer: sp);

        NameCache.Add(item, name);
        return name;
    }
}