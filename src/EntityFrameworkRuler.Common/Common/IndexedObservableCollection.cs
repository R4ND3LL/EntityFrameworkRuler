using System.Collections.ObjectModel;

namespace EntityFrameworkRuler.Common;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public sealed class IndexedObservableCollection<T, TKey> : ObservableCollection<T> {
    private readonly Func<T, TKey> keyGetter;
    private readonly Dictionary<TKey, T> dictionary;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IndexedObservableCollection(Func<T, TKey> keyGetter, IEqualityComparer<TKey> comparer = null) {
        this.keyGetter = keyGetter;
        dictionary = comparer != null ? new(comparer) : new Dictionary<TKey, T>();
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected override void InsertItem(int index, T item) {
        base.InsertItem(index, item);
        dictionary.Add(keyGetter(item), item);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected override void RemoveItem(int index) {
        var item = base[index];
        dictionary.Add(keyGetter(item), item);
        base.RemoveItem(index);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected override void ClearItems() {
        base.ClearItems();
        dictionary.Clear();
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public T FindByKey(TKey key) => dictionary.TryGetValue(key);
}