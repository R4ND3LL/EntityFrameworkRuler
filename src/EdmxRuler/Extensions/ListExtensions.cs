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
        return args.Contains(v);
    }

    internal static NavigationNamingRules ToPropertyRules(this PrimitiveNamingRules primitiveNamingRules) {
        var rules = new NavigationNamingRules();
        foreach (var schema in primitiveNamingRules.Schemas) {
            var r = new NavigationNamingRules();
            foreach (var table in schema.Tables) {
                var c = new ClassReference();
                c.Name = table.Name;
                r.Classes.Add(c);
                foreach (var columnNamer in table.Columns) {
                    var p = new NavigationRename();
                    p.Name = columnNamer.Name;
                    p.NewName = columnNamer.NewName;
                    c.Properties.Add(p);
                }
            }
        }

        return rules;
    }
}