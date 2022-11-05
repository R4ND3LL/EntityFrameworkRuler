using System.Diagnostics;
using EdmxRuler.RuleModels.PropertyRenaming;
using EdmxRuler.RuleModels.TableColumnRenaming;

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

    internal static ClassPropertyNamingRulesRoot ToPropertyRules(this TableAndColumnRulesRoot tableAndColumnRulesRoot) {
        var rules = new ClassPropertyNamingRulesRoot();
        foreach (var schema in tableAndColumnRulesRoot.Schemas) {
            var r = new ClassPropertyNamingRulesRoot();
            foreach (var table in schema.Tables) {
                var c = new ClassRenamer();
                c.Name = table.Name;
                r.Classes.Add(c);
                foreach (var columnNamer in table.Columns) {
                    var p = new PropertyRenamer();
                    p.Name = columnNamer.Name;
                    p.NewName = columnNamer.NewName;
                    c.Properties.Add(p);
                }
            }
        }

        return rules;
    }
}