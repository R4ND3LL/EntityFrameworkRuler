using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkRuler.Common.Annotations;

namespace EntityFrameworkRuler.Design.Extensions;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal static class AnnotationHelper {
    internal static bool IsValidAnnotation(string annotationKey) =>
        GetAnnotationIndex(annotationKey)?.Contains(annotationKey) == true;

    internal static AnnotationIndex GetAnnotationIndex(string annotationKey) {
        var parts = annotationKey.Split(":");
        var prefix = parts.Length == 2 ? parts[0] : null;
        var annotationDictionary = AnnotationHelperCore.ToDictionary(ResolveAnnotationType(prefix));
        return annotationDictionary;
    }

    internal static Type ResolveAnnotationType(string prefix) {
        return prefix switch {
            "Relational" => typeof(Microsoft.EntityFrameworkCore.Metadata.RelationalAnnotationNames),
            "Ruler" => typeof(RulerAnnotations),
            "Scaffolding" => typeof(Microsoft.EntityFrameworkCore.Metadata.Internal.ScaffoldingAnnotationNames),
            _ => typeof(Microsoft.EntityFrameworkCore.Metadata.Internal.CoreAnnotationNames)
        };
    }
}

