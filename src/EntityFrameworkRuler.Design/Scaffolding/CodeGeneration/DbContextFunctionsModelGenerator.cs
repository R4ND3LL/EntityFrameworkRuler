using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Scaffolding.Internal;
using EntityFrameworkRuler.Design.Services;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using TextTemplatingEngineHost = EntityFrameworkRuler.Design.Scaffolding.Internal.TextTemplatingEngineHost;

namespace EntityFrameworkRuler.Design.Scaffolding.CodeGeneration;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class DbContextFunctionsModelGenerator : RuledModelGeneratorBase, IRuledModelCodeGenerator {
    private readonly ModelCodeGeneratorDependencies dependencies;
    private readonly IServiceProvider serviceProvider;
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public DbContextFunctionsModelGenerator(
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
        var contextTemplate = RuledTemplatedModelGenerator.GetDbContextFunctionsFile(projectDir);
        var resultingFiles = new List<ScaffoldedFile>();
        if (contextTemplate.Exists) {
            if (modelEx?.GetFunctions() == null) return resultingFiles;

            reporter.WriteInformation($"RULED: Running template '{contextTemplate.Name}'...");
            var host = new TextTemplatingEngineHost(serviceProvider) {
                TemplateFile = contextTemplate.FullName
            };
            host.Initialize();
            host.Session.Add("Model", modelEx);
            host.Session.Add("Options", options);
            host.Session.Add("NamespaceHint", options.ContextNamespace ?? options.ModelNamespace);
            host.Session.Add("ProjectDefaultNamespace", options.RootNamespace);

            var generatedCode = GenerateCode(contextTemplate, host);

            if (string.IsNullOrWhiteSpace(generatedCode)) return resultingFiles;

            var functionFileName = options.ContextName + "." + "Functions" + host.Extension;
            if (designTimeRuleLoader.CodeGenOptions?.ContextDir != null)
                functionFileName = Path.Combine(designTimeRuleLoader.CodeGenOptions.ContextDir, functionFileName);
            resultingFiles.AddFile(functionFileName, generatedCode);
        } else reporter.WriteWarning($"{contextTemplate.Name} missing");

        return resultingFiles;
    }
}