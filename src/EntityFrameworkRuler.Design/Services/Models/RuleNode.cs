using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <summary> Rule node that provides enhanced access performance via indexed collections. </summary>
[DebuggerDisplay("{Rule}")]
public abstract class RuleNode<T, TParent> : IRuleItem where T : IRuleItem {
    /// <summary> Creates a rule node </summary>
    protected RuleNode(T rule, TParent parent) {
        Rule = rule;
        Parent = parent;
    }

    /// <summary> name of the item </summary>
    public string DbName => Rule.GetDbName().NullIfEmpty() ?? Rule.GetFinalName();

    /// <summary> The enclosed rule item </summary>
    public T Rule { get; }

    /// <summary> The parent rule node </summary>
    public TParent Parent { get; }

    /// <inheritdoc />
    public string GetExpectedEntityFrameworkName() => Rule.GetExpectedEntityFrameworkName();

    /// <inheritdoc />
    public string GetNewName() => Rule.GetNewName();

    /// <inheritdoc />
    public string GetDbName() => Rule.GetDbName();

    /// <inheritdoc />
    public string GetFinalName() => Rule.GetFinalName();

    /// <inheritdoc />
    public void SetFinalName(string value) => Rule.SetFinalName(value);

    /// <inheritdoc />
    public bool NotMapped => Rule.NotMapped;
}