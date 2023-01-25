using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Common.Annotations;

/// <summary> Annotation collection </summary>
public sealed class AnnotationCollection : ObservableCollection<AnnotationItem> {
    /// <summary> Creates an annotation collection </summary>
    public AnnotationCollection() { }
    //private SortedDictionary<string, AnnotationItem> dictionary;

    /// <summary> Add item with key and value </summary>
    public AnnotationItem Add(string key, object value) {
        var item = new AnnotationItem(key, value);
        Add(item);
        return item;
    }

    /// <summary> Remove item with key </summary>
    public AnnotationItem Remove(string key) {
        var item = Find(key);
        if (item == null) return null;
        Remove(item);
        return item;
    }
    /// <summary> Get item by value </summary>
    public AnnotationItem Find(string key) => this.FirstOrDefault(o => o.Key == key);

    /// <summary> Return true if the annotation with the given key exists </summary>
    public bool ContainsKey(string key) => this.Any(o => o.Key == key);

    /// <summary> Set item value </summary>
    public AnnotationItem Set(string key, object value) {
        var item = Find(key);
        if (item == null) {
            if (value == null) return null;
            return Add(key, value);
        }
        if (value == null) {
            Remove(item);
            return null;
        }
        item.Value = value;
        return item;
    }

    /// <summary>
    ///     Gets the value annotation with the given name, returning <see langword="null" /> if it does not exist.
    /// </summary>
    /// <param name="name">The key of the annotation to find.</param>
    /// <returns>
    ///     The value of the existing annotation if an annotation with the specified name already exists.
    ///     Otherwise, <see langword="null" />.
    /// </returns>
    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public object this[string name] {
        get => Find(name)?.Value;
        set {
            if (name == null) return;
            if (value == null) Remove(name);
            else Set(name, value);
        }
    }

    /// <inheritdoc />
    protected override void InsertItem(int index, AnnotationItem item) {
        base.InsertItem(index, item);
        //dictionary ??= new SortedDictionary<string, AnnotationItem>();
    }
    /// <inheritdoc />
    protected override void RemoveItem(int index) {
        base.RemoveItem(index);
    }


    internal SortedDictionary<string, object> ToDictionary() {
        if (Count == 0) return null;
        var d = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in this) {
            if (item.Key == null || item.Value == null) continue;
            d[item.Key] = item.Value;
        }
        return d;
    }
}

/// <summary> Annotation item </summary>
[DebuggerDisplay("{Key}: {Value}")]
public sealed class AnnotationItem {
    /// <summary> Create annotation item </summary>
    public AnnotationItem() { }

    /// <summary> Create annotation item </summary>
    public AnnotationItem(string key, object value) {
        Key = key;
        Value = value;
    }

    /// <summary> Gets or sets the Name </summary>
    public string Key { get; set; }

    /// <summary> Gets or sets the Value </summary>
    public object Value { get; set; }

    /// <summary> Gets the Value </summary>
    public object GetActualValue() {
        var v = Value;
        if (v is string s) v = s.NullIfEmpty();
        return v;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Key}: {GetActualValue()}";
}