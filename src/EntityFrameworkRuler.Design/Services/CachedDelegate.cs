namespace EntityFrameworkRuler.Design.Services;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class CachedDelegate<TKey, TValue> {
    private readonly Func<TKey, TValue> func;
    private readonly Dictionary<TKey, TValue> dictionary;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public CachedDelegate(Func<TKey, TValue> func, IEqualityComparer<TKey> comparer = null) {
        this.func = func;
        dictionary = new(comparer);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public TValue Invoke(TKey key) => dictionary.GetOrAddNew(key, Factory);

    private TValue Factory(TKey arg) => func(arg);
}