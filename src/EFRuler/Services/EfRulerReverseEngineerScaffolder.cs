using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using ICSharpUtilities = Microsoft.EntityFrameworkCore.Scaffolding.Internal.ICSharpUtilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Services;

/// <summary>
/// The purpose of this override is simply to provide the scaffolding context info to IRuleLoader, so that necessary
/// resources are available to the other scaffolding components in this library.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class EfRulerReverseEngineerScaffolder : ReverseEngineerScaffolder {
    private readonly IRuleLoader ruleLoader;

    /// <inheritdoc />
    public EfRulerReverseEngineerScaffolder(IDatabaseModelFactory databaseModelFactory,
        IScaffoldingModelFactory scaffoldingModelFactory,
        IModelCodeGeneratorSelector modelCodeGeneratorSelector,
        ICSharpUtilities cSharpUtilities,
        ICSharpHelper cSharpHelper,
        IDesignTimeConnectionStringResolver connectionStringResolver,
        IOperationReporter reporter,
        IRuleLoader ruleLoader) : base(databaseModelFactory,
        scaffoldingModelFactory,
        modelCodeGeneratorSelector,
        cSharpUtilities,
        cSharpHelper,
        connectionStringResolver,
        reporter) {
        this.ruleLoader = ruleLoader;
    }

    /// <inheritdoc />
    public override ScaffoldedModel ScaffoldModel(string connectionString, DatabaseModelFactoryOptions databaseOptions,
        ModelReverseEngineerOptions modelOptions, ModelCodeGenerationOptions codeOptions) {
        ruleLoader.SetCodeGenerationOptions(codeOptions).SetReverseEngineerOptions(modelOptions);
        var m = base.ScaffoldModel(connectionString, databaseOptions, modelOptions, codeOptions);
        return m;
    }
}