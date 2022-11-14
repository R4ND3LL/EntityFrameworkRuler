using EntityFrameworkRuler.Design.Services;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Design.Extensions;

/// <summary> Rule extensions </summary>
public static class RuleExtensions {
    /// <summary> Use the current scaffolding context to load the given type </summary>
    public static Type TryResolveType(this IRuleLoader ruleLoader, string clrTypeName, Type underlyingType) {
        var clrTypeNamespaceAndName = clrTypeName.SplitNamespaceAndName();
        var candidateNames = new List<string> { clrTypeName };

        if (ruleLoader?.CodeGenOptions != null) {
            // look for the type with a couple of different namespace variations
            if (clrTypeNamespaceAndName.namespaceName != ruleLoader.CodeGenOptions.ModelNamespace &&
                ruleLoader.CodeGenOptions.ModelNamespace.HasNonWhiteSpace())
                candidateNames.Add($"{ruleLoader.CodeGenOptions.ModelNamespace}.{clrTypeNamespaceAndName.name}");

            if (clrTypeNamespaceAndName.namespaceName != ruleLoader.CodeGenOptions.RootNamespace &&
                ruleLoader.CodeGenOptions.RootNamespace.HasNonWhiteSpace())
                candidateNames.Add($"{ruleLoader.CodeGenOptions.RootNamespace}.{clrTypeNamespaceAndName.name}");
        }

        // search also on just the basic name:
        if (clrTypeNamespaceAndName.namespaceName.HasNonWhiteSpace())
            candidateNames.Add(clrTypeNamespaceAndName.name);

        Type clrType = null;
        foreach (var candidateName in candidateNames) {
            clrType = Type.GetType(candidateName, false);
            if (clrType != null) continue;
            if (ruleLoader?.TargetAssemblies?.Count > 0)
                foreach (var targetAssembly in ruleLoader.TargetAssemblies) {
                    clrType = targetAssembly.GetType(candidateName, false);
                    if (clrType != null) break;
                }

            if (clrType != null) break;
        }

        if (clrType != null) return clrType;
        // try a full assembly scan without the namespace, but filter for enum types only with matching name
        if (!(ruleLoader?.TargetAssemblies?.Count > 0)) return null;
        foreach (var targetAssembly in ruleLoader.TargetAssemblies) {
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
}