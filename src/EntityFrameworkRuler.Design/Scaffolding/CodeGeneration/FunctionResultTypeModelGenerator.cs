using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using EntityFrameworkRuler.Design.Services;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using TextTemplatingEngineHost = EntityFrameworkRuler.Design.Scaffolding.Internal.TextTemplatingEngineHost;

namespace EntityFrameworkRuler.Design.Scaffolding.CodeGeneration;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class FunctionResultTypeModelGenerator : RuledModelGeneratorBase, IRuledModelCodeGenerator {
    private readonly ModelCodeGeneratorDependencies dependencies;
    private readonly IServiceProvider serviceProvider;
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public FunctionResultTypeModelGenerator(
        ModelCodeGeneratorDependencies dependencies,
        IOperationReporter reporter,
        IServiceProvider serviceProvider,
        IDesignTimeRuleLoader designTimeRuleLoader) : base(reporter) {
        this.dependencies = dependencies;
        this.serviceProvider = serviceProvider;
        this.designTimeRuleLoader = designTimeRuleLoader;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IList<ScaffoldedFile> GenerateModel(ModelEx modelEx, ModelCodeGenerationOptions options) {
        if (options.ContextName == null)
            throw new ArgumentException(
                CoreStrings.ArgumentPropertyNull(nameof(options.ContextName), nameof(options)), nameof(options));

        if (options.ConnectionString == null)
            throw new ArgumentException(
                CoreStrings.ArgumentPropertyNull(nameof(options.ConnectionString), nameof(options)), nameof(options));


        var projectDir = designTimeRuleLoader.GetProjectDir();
        var contextTemplate = RuledTemplatedModelGenerator.GetFunctionFile(projectDir);
        var resultingFiles = new List<ScaffoldedFile>();
        return resultingFiles;
        if (contextTemplate.Exists) {
            if (modelEx?.GetFunctions() == null) return resultingFiles;

            reporter.WriteInformation($"RULED: Running {contextTemplate.Name} template...");
            var host = new TextTemplatingEngineHost(serviceProvider) {
                TemplateFile = contextTemplate.FullName
            };
            foreach (var function in modelEx.GetFunctions().Where(o => o.MappedType.IsNullOrWhiteSpace() && (o.FunctionType == FunctionType.StoredProcedure || !o.IsScalar))) {
                int i = 1;

                foreach (var resultTable in function.Results) {
                    if (function.NoResultSet) continue;

                    var suffix = string.Empty;
                    if (function.Results.Count > 1) suffix = $"{i++}";

                    var typeName = function.Name + "Result" + suffix;

                    host.Initialize();
                    host.Session.Add("Function", modelEx);
                    host.Session.Add("ResultSet", resultTable);
                    host.Session.Add("Model", modelEx);
                    host.Session.Add("Options", options);
                    host.Session.Add("NamespaceHint", options.ContextNamespace ?? options.ModelNamespace);
                    host.Session.Add("ProjectDefaultNamespace", options.RootNamespace);

                    var generatedCode = GeneratedCode(contextTemplate, host);

                    if (string.IsNullOrWhiteSpace(generatedCode)) continue;

                    var functionFileName = typeName + host.Extension;
                    resultingFiles.Add(new() { Path = functionFileName, Code = generatedCode });
                }
            }
        } else reporter.WriteWarning($"{contextTemplate.Name} missing");

        return resultingFiles;
    }


}