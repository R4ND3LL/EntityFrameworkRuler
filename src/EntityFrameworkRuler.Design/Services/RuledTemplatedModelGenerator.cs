using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Common;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "NotAccessedField.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
// ReSharper disable once ClassNeverInstantiated.Global
internal class RuledTemplatedModelGenerator : IModelCodeGenerator {
    private readonly IMessageLogger reporter;
    private readonly IDesignTimeRuleLoader ruleLoader;
    private const string DbContextTemplate = "DbContext.t4";
    private const string EntityTypeTemplate = "EntityType.t4";
    private const string EntityTypeConfigurationTemplate = "EntityTypeConfiguration.t4";


    /// <summary> Creates an EF Ruler ModelCodeGenerator </summary>
    public RuledTemplatedModelGenerator(
        IMessageLogger reporter,
        IDesignTimeRuleLoader ruleLoader) {
        this.reporter = reporter;
        this.ruleLoader = ruleLoader;
    }

    internal static FileInfo GetEntityTypeConfigurationFile(string projectDir) {
        if (projectDir.IsNullOrWhiteSpace()) return null;
        return new(Path.Combine(projectDir!, TemplatesDirectory, EntityTypeConfigurationTemplate));
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