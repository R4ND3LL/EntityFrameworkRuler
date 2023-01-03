using System.Reflection;

namespace EntityFrameworkRuler.Common.Annotations;

/// <summary> Annotation helper </summary>
public static class AnnotationHelperCore {
    private static readonly Func<Type, AnnotationIndex> toDictionaryCached =
        RuleExtensions.Cached<Type, AnnotationIndex>(ToDictionaryCore);

    /// <summary> Return annotations for a given type (cached) </summary>
    public static AnnotationIndex ToDictionary(Type annotationType) => toDictionaryCached(annotationType);

    private static AnnotationIndex ToDictionaryCore(Type annotationType) {
        if (annotationType == null) return null;
        var dict = new AnnotationIndex();
        if (annotationType.IsEnum) {
            var values = Enum.GetValues(annotationType);
            foreach (var value in values) {
                var s = value.ToString();
                dict.Add(s, s);
            }

            return dict;
        }

        var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var fields = annotationType.GetFields(bindingFlags);
        foreach (var field in fields) {
            if (field.Name == nameof(EfRelationalAnnotationNames.Prefix)) continue;
            if (field.GetValue(null) is string value)
                dict.Add(field.Name, value);
        }

        return dict;
    }
}

/// <summary> Annotation dictionary for a specific type </summary>
public sealed class AnnotationIndex {
    private readonly Dictionary<string, string> annotationsByFieldName = new(StringComparer.Ordinal);
    private readonly HashSet<string> annotations = new(StringComparer.Ordinal);

    /// <summary> Add annotation item </summary>
    public void Add(string fieldName, string value) {
        annotationsByFieldName.Add(fieldName, value);
        annotations.Add(value);
    }

    /// <summary> Return true if annotation value exists </summary>
    public bool Contains(string annotationValue) => annotationValue != null && annotations.Contains(annotationValue);

    /// <summary> Return annotation value by name </summary>
    public string GetByField(string fieldName) => fieldName != null ? annotationsByFieldName.TryGetValue(fieldName) : null;

    /// <summary> Annotation field names </summary>
    public IEnumerable<string> FieldNames => annotationsByFieldName.Keys;

    /// <summary> Annotation values </summary>
    public IEnumerable<string> Values => annotations;
}