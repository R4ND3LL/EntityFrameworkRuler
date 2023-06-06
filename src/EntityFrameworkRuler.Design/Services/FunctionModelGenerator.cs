using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating;
using TextTemplatingEngineHost = EntityFrameworkRuler.Design.Scaffolding.Internal.TextTemplatingEngineHost;

namespace EntityFrameworkRuler.Design.Services;

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

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class FunctionModelGenerator : IRuledModelCodeGenerator {
    private readonly ModelCodeGeneratorDependencies dependencies;
    private readonly IOperationReporter reporter;
    private readonly IServiceProvider serviceProvider;
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public FunctionModelGenerator(
        ModelCodeGeneratorDependencies dependencies,
        IOperationReporter reporter,
        IServiceProvider serviceProvider,
        IDesignTimeRuleLoader designTimeRuleLoader) {
        this.dependencies = dependencies;
        this.reporter = reporter;
        this.serviceProvider = serviceProvider;
        this.designTimeRuleLoader = designTimeRuleLoader;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public string Language => "C#";

    private TemplatingEngine? engine;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual TemplatingEngine Engine => engine ??= new();

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
        string generatedCode;
        var resultingFiles = new List<ScaffoldedFile>();
        if (contextTemplate.Exists) {
            if (modelEx?.GetFunctions() == null) return resultingFiles;

            reporter.WriteInformation($"RULED: Running Functions.t4 template...");
            var host = new TextTemplatingEngineHost(serviceProvider) {
                TemplateFile = contextTemplate.FullName
            };
            host.Initialize();
            host.Session.Add("Model", modelEx);
            host.Session.Add("Options", options);
            host.Session.Add("NamespaceHint", options.ContextNamespace ?? options.ModelNamespace);
            host.Session.Add("ProjectDefaultNamespace", options.RootNamespace);

            generatedCode = Engine.ProcessTemplate(File.ReadAllText(contextTemplate.FullName), host);
            CheckEncoding(host.OutputEncoding);
            HandleErrors(host);

            if (string.IsNullOrWhiteSpace(generatedCode)) return resultingFiles;

            var functionFileName = options.ContextName + "Functions" + host.Extension;
            resultingFiles.Add(new() { Path = functionFileName, Code = generatedCode });
            //
            // foreach (var function in databaseModelEx.Functions) {
            //     host.Initialize();
            //     host.Session.Add("Model", function);
            //     host.Session.Add("Options", options);
            //     host.Session.Add("NamespaceHint", options.ContextNamespace ?? options.ModelNamespace);
            //     host.Session.Add("ProjectDefaultNamespace", options.RootNamespace);
            //
            //     generatedCode = Engine.ProcessTemplate(File.ReadAllText(contextTemplate.FullName), host);
            //     CheckEncoding(host.OutputEncoding);
            //     HandleErrors(host);
            //
            //     if (string.IsNullOrWhiteSpace(generatedCode)) continue;
            //
            //     var functionFileName = function.Name + host.Extension;
            //     resultingFiles.Add(new() { Path = functionFileName, Code = generatedCode });
            //     break;
            // }
        } else reporter.WriteWarning("Functions.t4 missing");

        return resultingFiles;
    }

    private void CheckEncoding(Encoding outputEncoding) {
        if (outputEncoding != Encoding.UTF8)
            reporter.WriteWarning(
                $"The encoding '{outputEncoding.WebName}' specified in the output directive will be ignored. EF Core always scaffolds files using the encoding 'utf-8'.");
    }

    private void HandleErrors(TextTemplatingEngineHost host) {
        foreach (CompilerError error in host.Errors) Write(error);

        if (host.Errors.HasErrors) throw new OperationException($"Processing '{host.TemplateFile}' failed.");
    }

    private void Write(CompilerError error) {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(error.FileName)) {
            builder.Append(error.FileName);

            if (error.Line > 0) {
                builder
                    .Append("(")
                    .Append(error.Line);

                if (error.Column > 0)
                    builder
                        .Append(",")
                        .Append(error.Column);

                builder.Append(")");
            }

            builder.Append(" : ");
        }

        builder
            .Append(error.IsWarning ? "warning" : "error")
            .Append(" ")
            .Append(error.ErrorNumber)
            .Append(": ")
            .AppendLine(error.ErrorText);

        if (error.IsWarning)
            reporter.WriteWarning(builder.ToString());
        else
            reporter.WriteError(builder.ToString());
    }
}