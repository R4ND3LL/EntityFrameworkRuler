using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Rules;

/// <summary> Base class for rule model items </summary>
[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
[DataContract]
public abstract class RuleBase : IRuleItem {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static bool Observable = false;

    /// <summary> Get the name that we expect EF will generate for this item. </summary>
    protected abstract string GetExpectedEntityFrameworkName();

    /// <summary> Gets the new name to give this element. </summary>
    protected abstract string GetNewName();

    /// <summary> Sets the conceptual name of the model. That is, the name that this element should have in the final reverse engineered model. </summary>
    protected abstract void SetFinalName(string value);

    /// <summary> If true, omit this column during the scaffolding process. </summary>
    public abstract bool NotMapped { get; set; }

    /// <summary> If false, omit this column during the scaffolding process. </summary>
    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public bool Mapped => !NotMapped;

    string IRuleItem.GetExpectedEntityFrameworkName() => GetExpectedEntityFrameworkName();
    string IRuleItem.GetNewName() => GetNewName();

    /// <inheritdoc />
    public string GetFinalName() => GetNewName().NullIfWhitespace() ?? GetExpectedEntityFrameworkName();
    void IRuleItem.SetFinalName(string value) => SetFinalName(value);

    #region Annotations

    /// <summary> Metadata annotations for this element. </summary>
    [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 99)]
    [DisplayName("Annotations"), Category("Mapping"), Description("Metadata annotations for this element.")]
    public SortedDictionary<string, object> Annotations { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets the value annotation with the given name, returning <see langword="null" /> if it does not exist.
    /// </summary>
    /// <param name="name">The key of the annotation to find.</param>
    /// <returns>
    ///     The value of the existing annotation if an annotation with the specified name already exists.
    ///     Otherwise, <see langword="null" />.
    /// </returns>
    [IgnoreDataMember, JsonIgnore, XmlIgnore]
    public virtual object this[string name] {
        get => FindAnnotation(name);
        set {
            if (name == null) return;
            if (value == null) {
                RemoveAnnotation(name);
            } else {
                SetAnnotation(name, value);
            }
        }
    }

    /// <summary>
    ///     Gets the annotation with the given name, returning <see langword="null" /> if it does not exist.
    /// </summary>
    /// <param name="name">The key of the annotation to find.</param>
    /// <returns>
    ///     The existing annotation if an annotation with the specified name already exists. Otherwise, <see langword="null" />.
    /// </returns>
    public virtual object FindAnnotation(string name) {
        return Annotations == null || name == null
            ? null
            : Annotations.TryGetValue(name, out var annotation)
                ? annotation
                : null;
    }

    /// <summary>
    ///     Sets the annotation stored under the given key. Overwrites the existing annotation if an
    ///     annotation with the specified name already exists.
    /// </summary>
    /// <param name="name">The key of the annotation to be added.</param>
    /// <param name="value">The value to be stored in the annotation.</param>
    public virtual void SetAnnotation(string name, object value) {
        var oldAnnotation = FindAnnotation(name);
        if (oldAnnotation != null
            && Equals(oldAnnotation, value)) {
            return;
        }

        SetAnnotation(name, value, oldAnnotation);
    }

    /// <summary>
    ///     Sets the annotation stored under the given key. Overwrites the existing annotation if an
    ///     annotation with the specified name already exists.
    /// </summary>
    protected virtual void SetAnnotation(string name,
        object annotation,
        object oldAnnotation) {
        Annotations[name] = annotation;
        OnAnnotationSet(name, annotation, oldAnnotation);
    }

    /// <summary>
    ///     Removes the given annotation from this object.
    /// </summary>
    /// <param name="name">The annotation to remove.</param>
    /// <returns>The annotation value that was removed.</returns>
    public virtual object RemoveAnnotation(string name) {
        var annotation = FindAnnotation(name);
        if (annotation == null) {
            return null;
        }

        Annotations.Remove(name);
        OnAnnotationSet(name, null, annotation);
        return annotation;
    }

    /// <summary> Called when an annotation was set or removed. </summary>
    protected virtual void OnAnnotationSet(string name, object annotation, object oldAnnotation) { }

    #endregion
}