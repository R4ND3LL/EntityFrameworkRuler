using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Common.Annotations;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using EntityFrameworkRuler.Design.Services;
using Humanizer;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Design.Extensions;

/// <summary> Rule extensions </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public static class RuleExtensions {
    private static readonly Dictionary<string, Type> typeCache = new();

    /// <summary> Use the current scaffolding context to load the given type </summary>
    public static Type TryResolveType(this IDesignTimeRuleLoader designTimeRuleLoader, string clrTypeName, Type underlyingType,
        IMessageLogger logger = null) {
        return typeCache.GetOrAddNew(clrTypeName, Factory);

        Type Factory(string arg) {
            try {
                var clrType = designTimeRuleLoader?.TryResolveTypeInternal(clrTypeName, underlyingType);
                if (clrType != null) return clrType;

                logger?.WriteWarning($"Type not found: {clrTypeName}");
                return null;
            } catch (Exception ex) {
                logger?.WriteWarning($"Error loading type '{clrTypeName}': {ex.Message}");
                return null;
            }
        }
    }


    /// <summary> Use the current scaffolding context to load the given type </summary>
    private static Type TryResolveTypeInternal(this IDesignTimeRuleLoader designTimeRuleLoader, string clrTypeName, Type underlyingType) {
        var clrTypeNamespaceAndName = clrTypeName.SplitNamespaceAndName();
        var candidateNames = new List<string> { clrTypeName };

        if (designTimeRuleLoader?.CodeGenOptions != null) {
            // look for the type with a couple of different namespace variations
            if (clrTypeNamespaceAndName.namespaceName != designTimeRuleLoader.CodeGenOptions.ModelNamespace &&
                designTimeRuleLoader.CodeGenOptions.ModelNamespace.HasNonWhiteSpace())
                candidateNames.Add($"{designTimeRuleLoader.CodeGenOptions.ModelNamespace}.{clrTypeNamespaceAndName.name}");

            if (clrTypeNamespaceAndName.namespaceName != designTimeRuleLoader.CodeGenOptions.RootNamespace &&
                designTimeRuleLoader.CodeGenOptions.RootNamespace.HasNonWhiteSpace())
                candidateNames.Add($"{designTimeRuleLoader.CodeGenOptions.RootNamespace}.{clrTypeNamespaceAndName.name}");
        }

        // search also on just the basic name:
        if (clrTypeNamespaceAndName.namespaceName.HasNonWhiteSpace())
            candidateNames.Add(clrTypeNamespaceAndName.name);

        Type clrType = null;
        foreach (var candidateName in candidateNames) {
            clrType = Type.GetType(candidateName, false);
            if (clrType != null) continue;
            if (designTimeRuleLoader?.TargetAssemblies != null)
                foreach (var targetAssembly in designTimeRuleLoader.TargetAssemblies) {
                    clrType = targetAssembly.GetType(candidateName, false);
                    if (clrType != null) break;
                }

            if (clrType != null) break;
        }

        if (clrType != null) return clrType;
        // try a full assembly scan without the namespace, but filter for enum types only with matching name
        if (designTimeRuleLoader?.TargetAssemblies == null) return null;
        foreach (var targetAssembly in designTimeRuleLoader.TargetAssemblies) {
            var allTypes = targetAssembly.GetTypes();
            var someTypes = allTypes.Where(o => o.IsEnum && o.Name == clrTypeNamespaceAndName.name).ToList();
            if (someTypes.Count > 0 && underlyingType != null) {
                // check  the underlying type
                if (someTypes.Any(o => o.UnderlyingSystemType == underlyingType))
                    someTypes = someTypes.Where(o => o.UnderlyingSystemType == underlyingType)
                        .ToList();
            }

            clrType = someTypes.FirstOrDefault();
            if (clrType != null) break;
        }

        return clrType;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static void ApplyAnnotations(this IMutableAnnotatable target, AnnotationCollection annotations, Func<string> nameGetter,
        IMessageLogger reporter, Predicate<AnnotationItem> filter = null) {
        if (target == null || annotations == null || annotations.Count == 0) return;
        foreach (var annotation in annotations) {
            if (filter != null && !filter(annotation)) continue;
            if (!IsValidAnnotation(annotation.Key)) {
                reporter.WriteWarning(
                    $"RULED: {nameGetter()} annotation '{annotation.Key}' is invalid. Skipping.");
                continue;
            }

            var v = annotation.GetActualValue();
            reporter.WriteVerbose(
                $"RULED: Applying {nameGetter()} annotation '{annotation.Key}' value '{v?.ToString()?.Truncate(15)}'.");
            target.SetOrRemoveAnnotation(annotation.Key, v);
#if DEBUG
            var v2 = target.FindAnnotation(annotation.Key)?.Value;
            Debug.Assert(v == v2);
#endif
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static void RemoveAnnotation(this IMutableAnnotatable target, string annotationName, bool assertRemoved = true) {
        //var scaffoldingDbSetName = EfScaffoldingAnnotationNames.DbSetName;
        Debug.Assert(IsValidAnnotation(annotationName));
        var removed = target.RemoveAnnotation(annotationName);
        Debug.Assert(!assertRemoved || removed != null);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    private static bool IsValidAnnotation(string annotationKey) =>
        AnnotationHelper.GetAnnotationIndex(annotationKey)?.Contains(annotationKey) == true;

    /// <summary> Get schema name dot name </summary>
    public static string GetFullName(this DatabaseObject dbo) => $"{dbo.Schema}.{dbo.Name}";
}