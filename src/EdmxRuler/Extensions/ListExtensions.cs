using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EdmxRuler.Rules.NavigationNaming;
using EdmxRuler.Rules.PrimitiveNaming;

// ReSharper disable MemberCanBePrivate.Global

namespace EdmxRuler.Extensions;

internal static class ListExtensions {
    [DebuggerStepThrough]
    public static bool IsNullOrEmpty<T>(this IList<T> list) { return list == null || list.Count == 0; }

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

    // internal static NavigationNamingRules ToPropertyRules(this PrimitiveNamingRules primitiveNamingRules) {
    //     var rules = new NavigationNamingRules();
    //     foreach (var schema in primitiveNamingRules.Schemas) {
    //         var r = new NavigationNamingRules();
    //         foreach (var classRename in schema.Tables) {
    //             var c = new ClassReference {
    //                 Name = classRename.NewName.CoalesceWhiteSpace(classRename.Name)
    //             };
    //             r.Classes.Add(c);
    //             foreach (var propertyRename in classRename.Columns) {
    //                 var p = new NavigationRename {
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

    public static void ForAll<T>(this IEnumerable<T> sequence, Action<T> action) {
        foreach (var item in sequence) action(item);
    }

    public static void AddRange<T>(this HashSet<T> c, IEnumerable<T> list) {
        foreach (var o in list) c.Add(o);
    }

#if NETCOREAPP3_1
    /// <summary> missing in .net 3.1 </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property) {
        return items.GroupBy(property).Select(x => x.First());
    }
#endif
    public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TValue : new() {
        return GetOrAddNew<TKey, TValue>(source, key, _ => new());
    }

    public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, Func<TKey, TValue> valueFactory) {
        if (source.TryGetValue(key, out var value)) return value;
        source.Add(key, value = valueFactory(key));
        return value;
    }

    /// <summary> remove items from the list that are program arg switches, but append those to the outbound switchArgs </summary>
    internal static List<string> RemoveSwitchArgs(this List<string> args, out List<string> switchArgs) {
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
}