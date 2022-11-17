using EntityFrameworkRuler.Rules.PrimitiveNaming;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;

// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <inheritdoc />
public class RuledTemplatedModelGenerator : IModelCodeGenerator {
    private readonly IDesignTimeRuleLoader ruleLoader;
    private PrimitiveNamingRules rules;
    private const string DbContextTemplate = "DbContext.t4";
    private const string EntityTypeTemplate = "EntityType.t4";
    private const string EntityTypeConfigurationTemplate = "EntityTypeConfiguration.t4";

    /// <summary> Creates an EF Ruler ModelCodeGenerator </summary>
    public RuledTemplatedModelGenerator(IDesignTimeRuleLoader ruleLoader) {
        this.ruleLoader = ruleLoader;
    }

    /// <inheritdoc />
    public string Language { get; private set; }

    /// <summary>
    ///     Gets the subdirectory under the project to look for templates in.
    /// </summary>
    /// <value>The subdirectory.</value>
    protected static string TemplatesDirectory { get; } = Path.Combine("CodeTemplates", "EFCore");

    /// <inheritdoc />
    public ScaffoldedModel GenerateModel(IModel model, ModelCodeGenerationOptions options) {
        var resultingFiles = new ScaffoldedModel {
        };
        if (!(ruleLoader.EfVersion?.Major >= 7)) return resultingFiles;
        // EF v7 supports entity config templating.
        rules ??= ruleLoader?.GetPrimitiveNamingRules();
        if (rules?.SplitEntityTypeConfigurations != true) return resultingFiles;

        var projectDir = ruleLoader?.GetProjectDir();
        if (projectDir.IsNullOrWhiteSpace()) return resultingFiles;


        var configurationTemplate = new FileInfo(Path.Combine(projectDir!, TemplatesDirectory, EntityTypeConfigurationTemplate));
        if (!configurationTemplate.Exists) {
            // wont be split by EF.  we should create the t4 file now
        }

        return resultingFiles;
    }
}