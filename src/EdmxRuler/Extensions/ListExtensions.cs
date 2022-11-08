using System;
using System.Collections.Generic;
using System.Diagnostics;
using EdmxRuler.RuleModels.NavigationNaming;
using EdmxRuler.RuleModels.PrimitiveNaming;

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

    internal static NavigationNamingRules ToPropertyRules(this PrimitiveNamingRules primitiveNamingRules) {
        var rules = new NavigationNamingRules();
        foreach (var schema in primitiveNamingRules.Schemas) {
            var r = new NavigationNamingRules();
            foreach (var classRename in schema.Tables) {
                var c = new ClassReference {
                    Name = classRename.NewName.CoalesceWhiteSpace(classRename.Name)
                };
                r.Classes.Add(c);
                foreach (var propertyRename in classRename.Columns) {
                    var p = new NavigationRename {
                        NewName = propertyRename.NewName
                    };
                    p.Name.Add(propertyRename.Name);
                    c.Properties.Add(p);
                }
            }
        }

        return rules;
    }

    public static void ForAll<T>(this IEnumerable<T> sequence, Action<T> action) {
        foreach (var item in sequence) action(item);
    }

    public static void AddRange<T>(this HashSet<T> c, IEnumerable<T> list) {
        foreach (var o in list) c.Add(o);
    }
}