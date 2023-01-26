using System.Collections;
using EntityFrameworkRuler.Rules;

// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Design.Services.Models;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class RuleIndex<T> : IEnumerable<T> where T : IRuleItem {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleIndex(Func<IEnumerable<T>> rules) {
        rulesGetter = rules;
    }

    private readonly Func<IEnumerable<T>> rulesGetter;
    private IList<T> rules;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IReadOnlyList<T> Rules => (IReadOnlyList<T>)(rules ??= rulesGetter().ToList());

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public int Count => Rules.Count;

    private Dictionary<string, T> rulesByFinalName;
    private Dictionary<string, T> rulesByDbName;

    /// <summary> Get rule by final element name </summary>
    public T GetByFinalName(string finalName) {
        if (finalName == null) return default;
        rulesByFinalName ??= InitializeRuleIndex(Rules, r => r.GetFinalName());
        return rulesByFinalName.TryGetValue(finalName);
    }


    /// <summary> Return true if rule exists in this collection with the DB name </summary>
    public bool ContainsDbName(string dbName) {
        if (dbName == null) return default;
        rulesByDbName ??= InitializeRuleIndex(Rules, r => r.GetDbName());
        return rulesByDbName.ContainsKey(dbName);
    }

    /// <summary> Get rule by DB name </summary>
    public T GetByDbName(string dbName) {
        if (dbName == null) return default;
        rulesByDbName ??= InitializeRuleIndex(Rules, r => r.GetDbName());
        return rulesByDbName.TryGetValue(dbName);
    }

    private static Dictionary<string, T> InitializeRuleIndex(IEnumerable<T> rules, Func<T, string> keyGetter) {
        Dictionary<string, T> rulesByName = new();
        foreach (var rule in rules) {
            var key = keyGetter(rule)?.Trim();
            if (key == null) continue;
            if (rulesByName.ContainsKey(key)) continue;
            rulesByName.Add(key, rule);
        }

        return rulesByName;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => Rules.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Rules.GetEnumerator();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public T this[int i] => Rules[i];

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public void Add(T r) {
        var list = (IList<T>)Rules;
        list.Add(r);
        if (rulesByDbName != null) {
            var key = r.GetDbName()?.Trim();
            if (key != null && !rulesByDbName.ContainsKey(key)) rulesByDbName.Add(key, r);
        }

        if (rulesByFinalName != null) {
            var key = r.GetFinalName()?.Trim();
            if (key != null && !rulesByFinalName.ContainsKey(key)) rulesByFinalName.Add(key, r);
        }
    }
}