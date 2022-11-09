using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
// ReSharper disable MemberCanBePrivate.Global

namespace EdmxRuler.Extensions;
internal static class ResourceExtensions {
    public static IEnumerable<string> GetEntityResourceDocuments(this Assembly assembly) {
        return GetResourceDocuments(assembly, "Applicator.EntityResources");
    }

    public static IEnumerable<string> GetResourceDocuments(this Assembly assembly, string folder) {
        var assemblyName = assembly.GetName().Name;
        var fullResourcePath = $"{assemblyName}.{folder}.";

        var names = assembly.GetManifestResourceNames();
        foreach (var name in names) {
            if (name.StartsWith(fullResourcePath, StringComparison.OrdinalIgnoreCase)) {
                yield return GetResourceText(assembly, name);
            }
        }
    }

    public static string GetResourceText(this Assembly assembly, string resourceName) {
        using var stream = GetResourceStream(assembly, resourceName);
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

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