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
    private readonly IRuleProvider ruleProvider;
    private PropertyTypeChangingRules typeChangingRules;

    /// <inheritdoc />
    public EfRulerRelationalScaffoldingModelFactory(
        IOperationReporter reporter,
        ICandidateNamingService candidateNamingService,
        IPluralizer pluralizer,
        ICSharpUtilities cSharpUtilities,
        IScaffoldingTypeMapper scaffoldingTypeMapper,
        IModelRuntimeInitializer modelRuntimeInitializer,
        IRuleProvider ruleProvider)
        : base(
            reporter,
            candidateNamingService,
            pluralizer,
            cSharpUtilities,
            scaffoldingTypeMapper,
            modelRuntimeInitializer) {
        this.ruleProvider = ruleProvider;
    }

    /// <inheritdoc />
    protected override TypeScaffoldingInfo GetTypeScaffoldingInfo(DatabaseColumn column) {
        var typeScaffoldingInfo = base.GetTypeScaffoldingInfo(column);
        typeChangingRules ??= ruleProvider?.GetPropertyTypeChangingRules() ?? new();


        var tableRule =
            typeChangingRules?.Classes?.FirstOrDefault(o => o.DbName == column.Table.Name || o.Name == column.Table.Name);

        if (tableRule == null) return typeScaffoldingInfo;

        var columnRule =
            tableRule?.Properties?.FirstOrDefault(o => o.DbName == column.Name || o.Name == column.Name);

        if (columnRule?.NewType.HasNonWhiteSpace() == true) {
            try {
                var clrTypeName = columnRule.NewType;
                var clrType = Type.GetType(clrTypeName, false);
                if (clrType == null) {
                    var assembly = Assembly.GetEntryAssembly();
                }

                if (clrType == null) {
                    EfRulerCandidateNamingService.DebugLog($"Type not found: {columnRule.NewType}");
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
                EfRulerCandidateNamingService.DebugLog(
                    $"Column rule applied: {tableRule.Name}.{columnRule.Name} type set to {columnRule.NewType}");
                return typeScaffoldingInfo;
            } catch (Exception ex) {
                EfRulerCandidateNamingService.DebugLog($"Error loading type '{columnRule.NewType}' reference: {ex.Message}");
            }
        }

        return typeScaffoldingInfo;
    }
}