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
/// The purpose of this override is simply to provide the scaffolding context info to IDesignTimeRuleLoader so that necessary
/// resources are available to the other scaffolding components in this library.
/// This is basically the first instance created during the scaffold process, so it's a good spot to pass over the
/// context info.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuledReverseEngineerScaffolder : ReverseEngineerScaffolder {
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;
    private readonly IExtraCodeGenerator extraCodeGenerator;
    private readonly IScaffoldingModelFactory scaffoldingModelFactory;
    private readonly IOperationReporter reporter;

    /// <inheritdoc />
    public RuledReverseEngineerScaffolder(IDatabaseModelFactory databaseModelFactory,
        IScaffoldingModelFactory scaffoldingModelFactory,
        IModelCodeGeneratorSelector modelCodeGeneratorSelector,
        ICSharpUtilities cSharpUtilities,
        ICSharpHelper cSharpHelper,
        IDesignTimeConnectionStringResolver connectionStringResolver,
        IOperationReporter reporter,
        IDesignTimeRuleLoader designTimeRuleLoader,
        IExtraCodeGenerator extraCodeGenerator,
        IServiceProvider serviceProvider
    ) : base(MakeDatabaseModelFactoryProxy(databaseModelFactory, reporter, serviceProvider),
        scaffoldingModelFactory,
        modelCodeGeneratorSelector,
        cSharpUtilities,
        cSharpHelper,
        connectionStringResolver,
        reporter) {
        this.scaffoldingModelFactory = scaffoldingModelFactory;
        this.designTimeRuleLoader = designTimeRuleLoader;
        this.extraCodeGenerator = extraCodeGenerator;
        this.reporter = reporter;
    }

    private static IDatabaseModelFactory MakeDatabaseModelFactoryProxy(IDatabaseModelFactory databaseModelFactory, IOperationReporter reporter, IServiceProvider serviceProvider) {
        try {
            var proxy = serviceProvider.GetConcrete<RuledDatabaseModelFactory>();
            return proxy;
        } catch (Exception ex) {
            reporter.WriteError($"Error creating proxy of IDatabaseModelFactory: {ex.Message}");
            return databaseModelFactory;
        }
    }

    /// <inheritdoc />
    public override ScaffoldedModel ScaffoldModel(string connectionString, DatabaseModelFactoryOptions databaseOptions,
        ModelReverseEngineerOptions modelOptions, ModelCodeGenerationOptions codeOptions) {
        designTimeRuleLoader.SetCodeGenerationOptions(ref codeOptions).SetReverseEngineerOptions(modelOptions);

        // IDatabaseModelFactory.Create() is called within, which is the actual Reverse Engineer process that builds the
        // ScaffoldedTable objects from the database.  The DatabaseModel is then converted to an IModel by
        // IScaffoldingModelFactory.Create(), and then code generated.
        var m = base.ScaffoldModel(connectionString, databaseOptions, modelOptions, codeOptions);

        if (scaffoldingModelFactory is RuledRelationalScaffoldingModelFactory ruledRelationalScaffoldingModelFactory) {
            var modelEx = ruledRelationalScaffoldingModelFactory.GetModel();
            var extras = extraCodeGenerator.GenerateCode(modelEx, codeOptions);
            if (extras?.Count > 0) {
                m.AdditionalFiles.AddRange(extras);
                reporter.WriteInformation($"RULER: Generated {extras.Count} extra files.");
            }
        }

        return m;
    }

    public override SavedModelFiles Save(ScaffoldedModel scaffoldedModel, string outputDir, bool overwriteFiles) {
        return base.Save(scaffoldedModel, outputDir, overwriteFiles);
    }
}