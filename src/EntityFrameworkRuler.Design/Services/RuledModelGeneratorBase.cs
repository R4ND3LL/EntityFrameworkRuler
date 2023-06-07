using System.CodeDom.Compiler;
using System.Text;
using EntityFrameworkRuler.Design.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Mono.TextTemplating;

namespace EntityFrameworkRuler.Design.Services;

public abstract class RuledModelGeneratorBase {
    protected IOperationReporter reporter;
    private TemplatingEngine engine;
    
    protected RuledModelGeneratorBase(IOperationReporter reporter) {
        this.reporter = reporter;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual TemplatingEngine Engine => engine ??= new();

    protected void CheckEncoding(Encoding outputEncoding) {
        if (outputEncoding != Encoding.UTF8)
            reporter.WriteWarning(
                $"The encoding '{outputEncoding.WebName}' specified in the output directive will be ignored. EF Core always scaffolds files using the encoding 'utf-8'.");
    }

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