using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Extension;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class ListExtensions {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [DebuggerStepThrough]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IList<T> list) { return list == null || list.Count == 0; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [DebuggerStepThrough]
    public static T TryGetElement<T>(this IList<T> list, int index)
        where T : class {
        return list?.Count > index ? list[index] : default;
    }

    /// <summary>
    /// In memory comparison extension for evaluating whether a value is NOT in a set of values.
    /// Like a SQL "where pkey not in (1,2,3,4)" statement. this extension allows you do write "pkey.NotIn(1,2,3,4)"
    /// </summary>
    [DebuggerStepThrough]
    public static bool NotIn<T>(this T v, params T[] args) { return !v.In(args); }

    /// <summary>
    /// In memory comparison extension for evaluating whether a value is in a set of values.
    /// Like a SQL "where pkey in (1,2,3,4)" statement. this extension allows you do write "pkey.In(1,2,3,4)"
    /// </summary>
    [DebuggerStepThrough]
    public static bool In<T>(this T v, params T[] args) {
        if (args == null || args.Length == 0) return false;
        foreach (var arg in args) {
            if (EqualityComparer<T>.Default.Equals(v, arg)) return true;
        }

        return false;
    }

    // internal static NavigationNamingRules ToPropertyRules(this DbContextRule primitiveNamingRules) {
    //     var rules = new NavigationNamingRules();
    //     foreach (var schema in primitiveNamingRules.Schemas) {
    //         var r = new NavigationNamingRules();
    //         foreach (var classRename in schema.Tables) {
    //             var c = new ClassReference {
    //                 Name = classRename.NewName.CoalesceWhiteSpace(classRename.Name)
    //             };
    //             r.Classes.Add(c);
    //             foreach (var propertyRename in classRename.Columns) {
    //                 var p = new NavigationRule {
    //                     NewName = propertyRename.NewName
    //                 };
    //                 p.Name.Add(propertyRename.Name);
    //                 c.Properties.Add(p);
    //             }
    //         }
    //     }
    //
    //     return rules;
    // }
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static void ForAll<T>(this IEnumerable<T> sequence, Action<T> action) {
        foreach (var item in sequence) action(item);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static void AddRange<T>(this IList<T> c, IEnumerable<T> list) {
        foreach (var o in list) c.Add(o);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static void AddRange<T>(this HashSet<T> c, IEnumerable<T> list) {
        foreach (var o in list) c.Add(o);
    }

    /// <summary> return true if all items are unique </summary>
    public static bool IsDistinct<T>(this IEnumerable<T> list) {
        var hash = new HashSet<T>();
        foreach (var o in list)
            if (!hash.Add(o))
                return false;
        return true;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static ObservableCollection<T> ToObservable<T>(this IEnumerable<T> c) {
        return new(c);
    }

#if LEGACY
    /// <summary> missing in .net 3.1 </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property) {
        return items.GroupBy(property).Select(x => x.First());
    }

#endif
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static HashSet<TSource> ToHashSetNew<TSource>(this IEnumerable<TSource> source) => source.ToHashSetNew(null);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static HashSet<TSource> ToHashSetNew<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return new(source, comparer);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TValue : new() {
        return GetOrAddNew(source, key, _ => new());
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, Func<TKey, TValue> valueFactory) {
        if (source.TryGetValue(key, out var value)) return value;
        source.Add(key, value = valueFactory(key));
        return value;
    }

    /// <summary> remove items from the list that are program arg switches, but append those to the outbound switchArgs </summary>
    public static List<string> RemoveSwitchArgs(this List<string> args, out List<string> switchArgs) {
        switchArgs = new();
        for (var i = args.Count - 1; i >= 0; i--) {
            var arg = args[i];
            var switchArg = arg.GetSwitchArg();
            if (switchArg == null) continue;
            args.RemoveAt(i);
            switchArgs.Add(switchArg);
        }

        return args;
    }

    /// <summary>
    /// This will search the given collection for an items that satisfies the given expression, and return the first
    /// index that returns true
    /// </summary>
    public static int IndexOf<TSource>(this IList<TSource> source, Predicate<TSource> searchFunc) {
        for (var i = 0; i < source.Count; i++) {
            var x = source[i];
            if (searchFunc(x)) return i;
        }

        return -1;
    }

    /// <summary>
    /// This will search the given collection for an items that satisfies the given expression, and return the first
    /// index that returns true
    /// </summary>
    public static int IndexOf(this IList source, Predicate<object> searchFunc) {
        for (var i = 0; i < source.Count; i++) {
            var x = source[i];
            if (searchFunc(x)) return i;
        }

        return -1;
    }

    /// <summary>
    /// This will search the given collection for an items that satisfies the given expression, and return the last
    /// index that returns true
    /// </summary>
    public static int LastIndexOf<TSource>(this IList<TSource> source, Predicate<TSource> searchFunc) {
        for (var i = source.Count - 1; i >= 0; i--) {
            var x = source[i];
            if (searchFunc(x)) return i;
        }

        return -1;
    }

    /// <summary>
    /// This will search the given collection for an items that satisfies the given expression, and return the last
    /// index that returns true
    /// </summary>
    public static int LastIndexOf(this IList source, Predicate<object> searchFunc) {
        for (var i = source.Count - 1; i >= 0; i--) {
            var x = source[i];
            if (searchFunc(x)) return i;
        }

        return -1;
    }

    /// <summary>
    /// Gets the value associated with the specified key or null if it does not exist
    /// </summary>
    [DebuggerStepThrough]
    public static TV TryGetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key) {
        if (dict != null && dict.TryGetValue(key, out var o)) return o;
        return default;
    }

    /// <summary>
    /// Gets the value associated with the specified key or null if it does not exist
    /// </summary>
    [DebuggerStepThrough]
    public static TV TryGetReadOnly<TK, TV>(this IReadOnlyDictionary<TK, TV> dict, TK key) {
        if (dict != null && dict.TryGetValue(key, out var o)) return o;
        return default;
    }

    /// <summary> add sorted routine using the native List class's BinarySearch method. Comparer is required. </summary>
    public static int AddSortedWithComparer<T>(this List<T> list, T item, IComparer<T> comparer) {
        if (comparer == null) throw new ArgumentNullException(nameof(comparer));
        if (list.Count == 0) {
            list.Add(item);
            return 0;
        }

        if (comparer.Compare(list[^1], item) <= 0) {
            // greater than last
            list.Add(item);
            return list.Count - 1;
        }

        if (comparer.Compare(list[0], item) >= 0) {
            // less than first
            list.Insert(0, item);
            return 0;
        }

        var index = list.BinarySearch(item, comparer);
        if (index < 0)
            index = ~index;
        list.Insert(index, item);
        return index;
    }
}