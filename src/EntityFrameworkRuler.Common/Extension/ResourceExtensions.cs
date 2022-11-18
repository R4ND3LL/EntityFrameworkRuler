using System.Diagnostics;
using System.Reflection;

// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Extension;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
internal static class ResourceExtensions {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static IEnumerable<string> GetEntityResourceDocuments(this Assembly assembly) {
        return GetResourceDocuments(assembly, "Applicator.EntityResources");
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static IEnumerable<string> GetResourceDocuments(this Assembly assembly, string folder) {
        var assemblyName = assembly.GetName().Name;
        var fullResourcePath = $"{assemblyName}.{folder}.";

        var names = assembly.GetManifestResourceNames();
        foreach (var name in names) {
            if (name.StartsWithIgnoreCase(fullResourcePath)) yield return GetResourceText(assembly, name);
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string GetResourceText(this Assembly assembly, string resourceName) {
        using var stream = GetResourceStream(assembly, resourceName);
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static Stream GetResourceStream(this Assembly assembly, string resourceName) {
        var stream = assembly.GetManifestResourceStream(resourceName);
#if DEBUG
        if (stream == null) {
            var names = assembly.GetManifestResourceNames();
            Debug.WriteLine($"Resource {resourceName} not found. Possible names include: {names}");
        }
#endif
        Debug.Assert(stream != null);
        return stream;
    }
}