using EntityFrameworkRuler.Common.Annotations;
using EntityFrameworkRuler.Design.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Design.Metadata;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public class UserTemplateContext {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public UserTemplateContext(ModelEx modelEx, ModelCodeGenerationOptions options, string namespaceHint, string projectDefaultNamespace) {
        Options = options;
        NamespaceHint = namespaceHint;
        ProjectDefaultNamespace = projectDefaultNamespace;
        this.modelEx = modelEx;
    }

    private readonly ModelEx modelEx;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IMutableModel Model => modelEx.Model;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ModelBuilderEx BuilderEx => modelEx.BuilderEx;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ModelBuilder Builder => modelEx.Builder;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ModelCodeGenerationOptions Options { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string NamespaceHint { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string ProjectDefaultNamespace { get; }

    /// <summary> The file path where the generated code will be saved.  This must be set by the template itself at runtime. </summary>
    public string OutputFileName { get; set; }

    /// <summary> Get all functions defined in the model. </summary>
    public IEnumerable<Function> GetFunctions() => modelEx.GetFunctions();

    /// <summary>
    ///     Gets all entity types defined in the model.
    /// </summary>
    /// <returns>All entity types defined in the model.</returns>
    public IEnumerable<IMutableEntityType> GetEntityTypes() => Model.GetEntityTypes();
}