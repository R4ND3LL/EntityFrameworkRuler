using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EdmxRuler.Common;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using ICSharpUtilities = Microsoft.EntityFrameworkCore.Scaffolding.Internal.ICSharpUtilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Services;

/// <inheritdoc />
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
#if DEBUG
        if (!Debugger.IsAttached) Debugger.Launch();
#endif
    }

    public override ScaffoldedModel ScaffoldModel(string connectionString, DatabaseModelFactoryOptions databaseOptions,
        ModelReverseEngineerOptions modelOptions, ModelCodeGenerationOptions codeOptions) {
        this.ruleLoader.SetCodeGenerationOptions(codeOptions);
        var m = base.ScaffoldModel(connectionString, databaseOptions, modelOptions, codeOptions);
        return m;
    }
}