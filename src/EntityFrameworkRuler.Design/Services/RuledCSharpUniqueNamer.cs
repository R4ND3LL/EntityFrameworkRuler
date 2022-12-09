using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Design.Services;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuledCSharpUniqueNamer<T, TRule> where T : notnull where TRule : class, IRuleItem {
    private readonly Func<T, NamedElementState<T, TRule>> nameGetter;
    private readonly ICSharpUtilities cSharpUtilities;
    private readonly Func<string, string> singularizePluralizer;
    private readonly HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<T, NamedElementState<T, TRule>> namedTablesByTable = new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledCSharpUniqueNamer(Func<T, NamedElementState<T, TRule>> nameGetter, ICSharpUtilities cSharpUtilities,
        Func<string, string> singularizePluralizer)
        : this(nameGetter, null, cSharpUtilities, singularizePluralizer) {
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuledCSharpUniqueNamer(Func<T, NamedElementState<T, TRule>> nameGetter,
        IEnumerable<string> usedNames,
        ICSharpUtilities cSharpUtilities,
        Func<string, string> singularizePluralizer) {
        this.nameGetter = nameGetter;
        this.cSharpUtilities = cSharpUtilities;
        this.singularizePluralizer = singularizePluralizer;
        if (usedNames == null) return;
        foreach (var name in usedNames) this.usedNames.Add(name);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string GetName(T item) {
        // note, cannot cache name mappings by STRING because names are dynamically set with
        // rules and the rules may not always be the same.

        var state = namedTablesByTable.GetOrAddNew(item, NameStateFactory);

        usedNames.Add(state.Name);
        return state.Name;
    }

    private NamedElementState<T, TRule> NameStateFactory(T item) {
        var state = nameGetter(item);

        // if allowed to pluralize/singularize name, i.e. it was not explicitly set by rule, then pass the
        // singularizePluralizer. otherwise pass null for the singularizePluralizer.
        // If this is not done, the explicitly defined names may be altered by the singularizePluralizer.
        var sp = state.IsFrozen ? null : singularizePluralizer;

        var input = cSharpUtilities.GenerateCSharpIdentifier(state.Name,
            existingIdentifiers: null,
            singularizePluralizer: sp);

        // enumerate name AFTER pluralization
        var name = input;
        var suffix = 1;
        while (usedNames.Contains(name)) name = input + suffix++;

        state.Name = name;
        return state;
    }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[DebuggerDisplay("{Name}")]
public struct NamedElementState<T, TRule> where T : notnull where TRule : class, IRuleItem {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public NamedElementState(string name, T element, TRule rule = null, bool isFrozen = false) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Element = element ?? throw new ArgumentNullException(nameof(element));
        Rule = rule;
        IsFrozen = isFrozen;
    }

    /// <summary> The final name given to this entity model element. </summary>
    public string Name { get; set; }

    /// <summary> The entity element that is being named (table, property, navigation). </summary>
    public T Element { get; }

    /// <summary> If any, the rule associated with this element. </summary>
    public TRule Rule { get; }

    /// <summary> If true, do NOT pluralize/singularize the element name. </summary>
    public bool IsFrozen { get; }

    /// <summary> implicitly convert to string </summary>
    public static implicit operator string(NamedElementState<T, TRule> o) => o.ToString();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public override string ToString() => Name;
}