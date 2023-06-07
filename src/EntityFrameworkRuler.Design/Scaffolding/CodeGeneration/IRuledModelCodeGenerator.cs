using EntityFrameworkRuler.Design.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Design.Scaffolding.CodeGeneration;

/// <summary> Code generator interface </summary>
public interface IRuledModelCodeGenerator {
    /// <summary>
    ///     Generates code for a model.
    /// </summary>
    /// <param name="databaseModelEx"></param>
    /// <param name="options">The options to use during generation.</param>
    /// <returns>The generated model.</returns>
    IList<ScaffoldedFile> GenerateModel(ModelEx databaseModelEx,
        ModelCodeGenerationOptions options);
}