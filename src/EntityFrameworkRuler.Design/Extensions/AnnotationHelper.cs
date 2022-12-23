using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkRuler.Common.Annotations;

namespace EntityFrameworkRuler.Design.Extensions;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal static class AnnotationHelper {
    private static readonly Func<Type, AnnotationIndex> toDictionaryCached =
        RuleExtensions.Cached<Type, AnnotationIndex>(ToDictionaryCore);

    internal static bool IsValidAnnotation(string annotationKey) =>
        GetAnnotationIndex(annotationKey)?.Contains(annotationKey) == true;

    internal static AnnotationIndex GetAnnotationIndex(string annotationKey) {
        var parts = annotationKey.Split(":");
        var prefix = parts.Length == 2 ? parts[0] : null;
        var annotationDictionary = AnnotationHelper.ToDictionary(ResolveAnnotationType(prefix));
        return annotationDictionary;
    }

    internal static Type ResolveAnnotationType(string prefix) {
        return prefix switch {
            "Relational" => typeof(Microsoft.EntityFrameworkCore.Metadata.RelationalAnnotationNames),
            "Ruler" => typeof(RulerAnnotations),
            _ => typeof(Microsoft.EntityFrameworkCore.Metadata.Internal.CoreAnnotationNames)
        };
    }

    internal static AnnotationIndex ToDictionary(Type annotationType) => toDictionaryCached(annotationType);

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
            if (field.GetValue(null) is string value)
                dict.Add(field.Name, value);
        }

        return dict;
    }
}

internal sealed class AnnotationIndex {
    private readonly Dictionary<string, string> annotationsByFieldName = new(StringComparer.Ordinal);
    private readonly HashSet<string> annotations = new(StringComparer.Ordinal);

    public void Add(string fieldName, string value) {
        annotationsByFieldName.Add(fieldName, value);
        annotations.Add(value);
    }

    public bool Contains(string annotationValue) => annotationValue != null && annotations.Contains(annotationValue);
    public string GetByField(string fieldName) => fieldName != null ? annotationsByFieldName.TryGetValue(fieldName) : null;
}