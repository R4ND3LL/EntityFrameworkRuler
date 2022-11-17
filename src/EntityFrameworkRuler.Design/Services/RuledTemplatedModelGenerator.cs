using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Rules.PrimitiveNaming;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal class RuledTemplatedModelGenerator : IModelCodeGenerator {
    private readonly IOperationReporter reporter;
    private readonly IDesignTimeRuleLoader ruleLoader;
    private const string DbContextTemplate = "DbContext.t4";
    private const string EntityTypeTemplate = "EntityType.t4";
    private const string EntityTypeConfigurationTemplate = "EntityTypeConfiguration.t4";


    /// <summary> Creates an EF Ruler ModelCodeGenerator </summary>
    public RuledTemplatedModelGenerator(
        IOperationReporter reporter,
        IDesignTimeRuleLoader ruleLoader) {
        this.reporter = reporter;
        this.ruleLoader = ruleLoader;
    }

    internal static FileInfo GetEntityTypeConfigurationFile(string projectDir) {
        if (projectDir.IsNullOrWhiteSpace()) return null;
        return new FileInfo(Path.Combine(projectDir!, TemplatesDirectory, EntityTypeConfigurationTemplate));
    }

    /// <summary>
    ///     Gets the subdirectory under the project to look for templates in.
    /// </summary>
    /// <value>The subdirectory.</value>
    protected static string TemplatesDirectory { get; } = Path.Combine("CodeTemplates", "EFCore");

    public string Language => null;

    public ScaffoldedModel GenerateModel(IModel model, ModelCodeGenerationOptions options) {
        throw new NotImplementedException();
    }
}