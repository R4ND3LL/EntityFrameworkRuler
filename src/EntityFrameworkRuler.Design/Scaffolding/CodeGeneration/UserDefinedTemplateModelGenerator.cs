﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Services;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using TextTemplatingEngineHost = EntityFrameworkRuler.Design.Scaffolding.Internal.TextTemplatingEngineHost;

namespace EntityFrameworkRuler.Design.Scaffolding.CodeGeneration;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class UserDefinedTemplateModelGenerator : RuledModelGeneratorBase, IRuledModelCodeGenerator {
    private readonly ModelCodeGeneratorDependencies dependencies;
    private readonly IServiceProvider serviceProvider;
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public UserDefinedTemplateModelGenerator(
        ModelCodeGeneratorDependencies dependencies,
        IOperationReporter reporter,
        IServiceProvider serviceProvider,
        IDesignTimeRuleLoader designTimeRuleLoader) : base(reporter) {
        this.dependencies = dependencies;
        this.serviceProvider = serviceProvider;
        this.designTimeRuleLoader = designTimeRuleLoader;
    }

    private static Regex paramRegex = new Regex(@"<#@\s+parameter\s+name=""(?<name>\w+)""\s+type=""(?<type>[^""]+)""\s*#>");

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IList<ScaffoldedFile> GenerateModel(ModelEx modelEx, ModelCodeGenerationOptions options) {
        if (options.ContextName == null)
            throw new ArgumentException(
                CoreStrings.ArgumentPropertyNull(nameof(options.ContextName), nameof(options)), nameof(options));

        if (options.ConnectionString == null)
            throw new ArgumentException(
                CoreStrings.ArgumentPropertyNull(nameof(options.ConnectionString), nameof(options)), nameof(options));


        var projectDir = designTimeRuleLoader.GetProjectDir();

        var fields = typeof(RuledTemplatedModelGenerator).GetFields(BindingFlags.Default | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)
            .Where(o => o.FieldType == typeof(string) && o.IsStatic)
            .Select(o => o.GetValue(null) as string)
            .Where(o => o?.EndsWith(".t4") == true)
            .ToHashSet();

        Debug.Assert(fields.Count >= 7);
        var namespaceHint = options.ContextNamespace ?? options.ModelNamespace;

        var contextTemplates = RuledTemplatedModelGenerator.GetAllTemplateFolderFiles(projectDir);
        var resultingFiles = new List<ScaffoldedFile>();
        foreach (var contextTemplate in contextTemplates) {
            try {
                if (fields.Contains(contextTemplate.Name))
                    continue; // this is a normal system template.  skip it as it is handled elsewhere

                if (!contextTemplate.Exists) {
                    reporter.WriteWarning($"{contextTemplate.Name} missing");
                    continue;
                }

                if (modelEx?.GetFunctions() == null) return resultingFiles;


                var content = File.ReadAllText(contextTemplate.FullName) ?? string.Empty;
                var parameters = paramRegex.Matches(content)
                    .Select(parameter => (name: parameter.Groups["name"] is { } g && g.Success ? g.Value : null,
                        type: parameter.Groups["type"] is { } g2 && g2.Success ? g2.Value : null))
                    .Where(o => o.name.HasNonWhiteSpace())
                    .ToDictionary(o => o.name, o => o.type, StringComparer.OrdinalIgnoreCase);
                var hasContext = parameters.ContainsKey("Context");
                if (!hasContext) {
                    reporter.WriteInformation(
                        $"Template '{contextTemplate.Name}' does not have a 'Context' parameter (type 'EntityFrameworkRuler.Design.Metadata.UserTemplateContext') so it will not be run.");
                    continue;
                }

                var hasEntityTypeParam = parameters.ContainsKey("EntityType");

                var host = new TextTemplatingEngineHost(serviceProvider) {
                    TemplateFile = contextTemplate.FullName
                };

                var context = new UserTemplateContext(modelEx, options, namespaceHint, options.RootNamespace);

                reporter.WriteInformation($"RULED: Running template '{contextTemplate.Name}'...");

                if (hasEntityTypeParam) {
                    // execute per entity
                    foreach (var entityType in modelEx.Model.GetEntityTypes()) {
                        host.Initialize();
                        SetParameters(host, parameters, contextTemplate, context, entityType);
                        RunGenerateCode(contextTemplate, host, resultingFiles, context);
                    }
                } else {
                    // execute once
                    SetParameters(host, parameters, contextTemplate, context, null);
                    RunGenerateCode(contextTemplate, host, resultingFiles, context);
                }
            } catch (Exception ex) {
                reporter.WriteError($"User defined template code gen failed: " + ex.Message);
            }
        }

        return resultingFiles;

        void SetParameters(TextTemplatingEngineHost host, Dictionary<string, string> parameters,
            FileInfo contextTemplate, UserTemplateContext context, IMutableEntityType entityType) {
            foreach (var kvp in parameters) {
                var name = kvp.Key;
                var type = kvp.Value;
                switch (name.ToLower()) {
                    case "context":
                        host.Session.Add(name, context);
                        break;
                    case "model":
                        if (type.EndsWith("ModelEx"))
                            host.Session.Add(name, modelEx);
                        else
                            host.Session.Add(name, (Microsoft.EntityFrameworkCore.Metadata.IModel)modelEx.Model);
                        break;
                    case "options":
                        host.Session.Add(name, options);
                        break;
                    case "namespacehint":
                        host.Session.Add(name, namespaceHint);
                        break;
                    case "projectdefaultnamespace":
                        host.Session.Add(name, options.RootNamespace);
                        break;
                    case "entitytype":
                        host.Session.Add(name, entityType);
                        break;
                    default:
                        reporter.WriteWarning(
                            $"RULED: Unhandled parameter '{name}' found in template file '{contextTemplate.Name}'");
                        break;
                }
            }
        }
    }

    private void RunGenerateCode(FileInfo contextTemplate, TextTemplatingEngineHost host, List<ScaffoldedFile> resultingFiles, UserTemplateContext userTemplateContext) {
        userTemplateContext.OutputFileName = null;
        var generatedCode = GeneratedCode(contextTemplate, host);
        if (generatedCode.IsNullOrWhiteSpace()) return;

        var outputFileName = userTemplateContext.OutputFileName.CoalesceWhiteSpace(contextTemplate.Name[..^3]);
        if (!Path.HasExtension(outputFileName)) outputFileName += host.Extension;

        if (designTimeRuleLoader.CodeGenOptions?.ContextDir != null)
            outputFileName = Path.Combine(designTimeRuleLoader.CodeGenOptions.ContextDir, outputFileName);

        resultingFiles.Add(new() { Path = outputFileName, Code = generatedCode });
    }
}