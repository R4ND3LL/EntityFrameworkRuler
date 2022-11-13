using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EdmxRuler.Common;
using EdmxRuler.Extensions;
using EdmxRuler.Rules.PropertyTypeChanging;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using ICSharpUtilities = Microsoft.EntityFrameworkCore.Scaffolding.Internal.ICSharpUtilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Services;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class EfRulerRelationalScaffoldingModelFactory : RelationalScaffoldingModelFactory {
    private readonly IOperationReporter reporter;
    private readonly IRuleLoader ruleLoader;
    private PropertyTypeChangingRules typeChangingRules;

    /// <inheritdoc />
    public EfRulerRelationalScaffoldingModelFactory(
        IOperationReporter reporter,
        ICandidateNamingService candidateNamingService,
        IPluralizer pluralizer,
        ICSharpUtilities cSharpUtilities,
        IScaffoldingTypeMapper scaffoldingTypeMapper,
        IModelRuntimeInitializer modelRuntimeInitializer,
        IRuleLoader ruleLoader)
        : base(
            reporter,
            candidateNamingService,
            pluralizer,
            cSharpUtilities,
            scaffoldingTypeMapper,
            modelRuntimeInitializer) {
        this.reporter = reporter;
        this.ruleLoader = ruleLoader;
    }

    /// <inheritdoc />
    protected override TypeScaffoldingInfo GetTypeScaffoldingInfo(DatabaseColumn column) {
        var typeScaffoldingInfo = base.GetTypeScaffoldingInfo(column);
        typeChangingRules ??= ruleLoader?.GetPropertyTypeChangingRules() ?? new();


        var tableRule =
            typeChangingRules?.Classes?.FirstOrDefault(o => o.DbName == column.Table.Name || o.Name == column.Table.Name);

        if (tableRule == null) return typeScaffoldingInfo;

        var columnRule =
            tableRule?.Properties?.FirstOrDefault(o => o.DbName == column.Name || o.Name == column.Name);

        if (columnRule?.NewType.HasNonWhiteSpace() == true) {
            try {
                var clrTypeName = columnRule.NewType;
                var clrTypeNamespaceAndName = columnRule.NewType.SplitNamespaceAndName();
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

                // search just the basic name:
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

                if (clrType == null) {
                    // try a full assembly scan without the namespace, but filter for enum types only with matching name
                    if (ruleLoader?.TargetAssemblies?.Count > 0)
                        foreach (var targetAssembly in ruleLoader.TargetAssemblies) {
                            var allTypes = targetAssembly.GetTypes();
                            var someTypes = allTypes.Where(o => o.IsEnum && o.Name == clrTypeNamespaceAndName.name).ToList();
                            if (someTypes.Count > 0) {
                                // check  the underlying type
                                if (someTypes.Any(o => o.UnderlyingSystemType == typeScaffoldingInfo?.ClrType))
                                    someTypes = someTypes.Where(o => o.UnderlyingSystemType == typeScaffoldingInfo?.ClrType).ToList();
                            }

                            clrType = someTypes.FirstOrDefault();
                            if (clrType != null) break;
                        }
                }

                if (clrType == null) {
                    WriteWarning($"Type not found: {columnRule.NewType}");
                    return typeScaffoldingInfo;
                }

                // Regenerate the TypeScaffoldingInfo based on our new CLR type.
                typeScaffoldingInfo = new(
                    clrType,
                    typeScaffoldingInfo?.IsInferred ?? false,
                    typeScaffoldingInfo?.ScaffoldUnicode,
                    typeScaffoldingInfo?.ScaffoldMaxLength,
                    typeScaffoldingInfo?.ScaffoldFixedLength,
                    typeScaffoldingInfo?.ScaffoldPrecision,
                    typeScaffoldingInfo?.ScaffoldScale);
                WriteVerbose($"Column rule applied: {tableRule.Name}.{columnRule.Name} type set to {columnRule.NewType}");
                return typeScaffoldingInfo;
            } catch (Exception ex) {
                WriteWarning($"Error loading type '{columnRule.NewType}' reference: {ex.Message}");
            }
        }

        return typeScaffoldingInfo;
    }

    internal void WriteWarning(string msg) {
        reporter?.WriteWarning(msg);
        EfRulerCandidateNamingService.DebugLog(msg);
    }

    internal void WriteVerbose(string msg) {
        reporter?.WriteVerbose(msg);
        EfRulerCandidateNamingService.DebugLog(msg);
    }
}