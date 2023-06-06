using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Common;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Design.Services;
 
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "NotAccessedField.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
// ReSharper disable once ClassNeverInstantiated.Global
internal class RuledTemplatedModelGenerator {
    private const string DbContextTemplate = "DbContext.t4";
    private const string EntityTypeTemplate = "EntityType.t4";
    private const string EntityTypeConfigurationTemplate = "EntityTypeConfiguration.t4";
    private const string FunctionTemplate = "Functions.t4";


    internal static FileInfo GetEntityTypeConfigurationFile(string projectDir) {
        if (projectDir.IsNullOrWhiteSpace()) return null;
        return new(Path.Combine(projectDir!, TemplatesDirectory, EntityTypeConfigurationTemplate));
    }
    internal static FileInfo GetFunctionFile(string projectDir) {
        if (projectDir.IsNullOrWhiteSpace()) return null;
        return new(Path.Combine(projectDir!, TemplatesDirectory, FunctionTemplate));
    }

    /// <summary>
    ///     Gets the subdirectory under the project to look for templates in.
    /// </summary>
    /// <value>The subdirectory.</value>
    protected static string TemplatesDirectory { get; } = Path.Combine("CodeTemplates", "EFCore");
 
}