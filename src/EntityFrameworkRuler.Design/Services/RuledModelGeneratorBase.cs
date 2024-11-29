using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EntityFrameworkRuler.Design.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Mono.TextTemplating;
using OperationException = Microsoft.EntityFrameworkCore.Design.OperationException;

namespace EntityFrameworkRuler.Design.Services;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public abstract class RuledModelGeneratorBase {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected IOperationReporter reporter;

    private TemplatingEngine engine;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected RuledModelGeneratorBase(IOperationReporter reporter) {
        this.reporter = reporter;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual TemplatingEngine Engine => engine ??= new();

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual string GenerateCode(FileInfo contextTemplate, TextTemplatingEngineHost host, string text = null) {
        text ??= File.ReadAllText(contextTemplate.FullName);
#if NET8_0_OR_GREATER
        var generatedCode = Engine.ProcessTemplateAsync(text, host).GetAwaiter().GetResult();
#else
        var generatedCode = Engine.ProcessTemplate(text, host);
#endif
        CheckEncoding(host.OutputEncoding);
        HandleErrors(host);
        return generatedCode;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected void CheckEncoding(Encoding outputEncoding) {
        if (outputEncoding != Encoding.UTF8)
            reporter.WriteWarning(
                $"The encoding '{outputEncoding.WebName}' specified in the output directive will be ignored. EF Core always scaffolds files using the encoding 'utf-8'.");
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected void HandleErrors(TextTemplatingEngineHost host) {
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