using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using ICSharpUtilities = Microsoft.EntityFrameworkCore.Scaffolding.Internal.ICSharpUtilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <summary>
/// The purpose of this override is simply to provide the scaffolding context info to IRuleLoader, so that necessary
/// resources are available to the other scaffolding components in this library.
/// This is basically the first instance created during the scaffold process, so it's a good spot to pass over the
/// context info.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuledReverseEngineerScaffolder : ReverseEngineerScaffolder {
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;

    /// <inheritdoc />
    public RuledReverseEngineerScaffolder(IDatabaseModelFactory databaseModelFactory,
        IScaffoldingModelFactory scaffoldingModelFactory,
        IModelCodeGeneratorSelector modelCodeGeneratorSelector,
        ICSharpUtilities cSharpUtilities,
        ICSharpHelper cSharpHelper,
        IDesignTimeConnectionStringResolver connectionStringResolver,
        IOperationReporter reporter,
        IDesignTimeRuleLoader designTimeRuleLoader) : base(databaseModelFactory,
        scaffoldingModelFactory,
        modelCodeGeneratorSelector,
        cSharpUtilities,
        cSharpHelper,
        connectionStringResolver,
        reporter) {
        this.designTimeRuleLoader = designTimeRuleLoader;
    }

    /// <inheritdoc />
    public override ScaffoldedModel ScaffoldModel(string connectionString, DatabaseModelFactoryOptions databaseOptions,
        ModelReverseEngineerOptions modelOptions, ModelCodeGenerationOptions codeOptions) {
        designTimeRuleLoader.SetCodeGenerationOptions(codeOptions).SetReverseEngineerOptions(modelOptions);
        var m = base.ScaffoldModel(connectionString, databaseOptions, modelOptions, codeOptions);
        return m;
    }
}