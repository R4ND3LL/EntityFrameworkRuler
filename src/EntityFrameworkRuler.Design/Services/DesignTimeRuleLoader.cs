using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Scaffolding;
using EntityFrameworkRuler.Design.Scaffolding.CodeGeneration;
using EntityFrameworkRuler.Design.Services.Models;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

namespace EntityFrameworkRuler.Design.Services;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DesignTimeRuleLoader : IDesignTimeRuleLoader {
    private readonly IMessageLogger logger;
    private readonly IRuleLoader ruleLoader;

    /// <summary> Creates the rule loader </summary>
    public DesignTimeRuleLoader(IMessageLogger logger, IRuleLoader ruleLoader) {
        this.logger = logger ?? new ConsoleMessageLogger();
        this.ruleLoader = ruleLoader ?? throw new ArgumentNullException(nameof(ruleLoader));

        var efAssembly = typeof(CandidateNamingService).Assembly;
        var efAssemblyName = efAssembly?.GetName();
        EfVersion = efAssemblyName?.Version;

        var thisAssembly = typeof(DesignTimeRuleLoader).Assembly;
        var thisAssemblyName = thisAssembly?.GetName();
        EfRulerVersion = thisAssemblyName?.Version;

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (EfVersion != null) this.logger.WriteInformation($"EF Ruler v{EfRulerVersion} detected Entity Framework v{EfVersion}");
        else this.logger.WriteInformation($"EF Ruler v{EfRulerVersion} could not detect the EF version");
    }

    /// <summary> The detected entity framework version.  </summary>
    public Version EfVersion { get; protected set; }

    /// <summary> This assembly version.  </summary>
    public Version EfRulerVersion { get; protected set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected bool IsEf6 => EfVersion?.Major == 6;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected bool IsEf7 => EfVersion?.Major == 7;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected bool IsEf8 => EfVersion?.Major == 8;

    private List<DbContextRuleNode> loadedRules;

    /// <inheritdoc />
    public ModelCodeGenerationOptionsEx CodeGenOptions { get; private set; }

    /// <inheritdoc />
    public ModelReverseEngineerOptions ReverseEngineerOptions { get; private set; }

    /// <inheritdoc />
    public string SolutionPath { get; private set; }


    private IList<TargetAssembly> targetAssemblies;


    /// <inheritdoc />
    public IEnumerable<Assembly> TargetAssemblies =>
        targetAssemblies?
            .Select(o => o.Assembly)
            .Where(o => o != null)
            .Distinct() ??
        Array.Empty<Assembly>();


    /// <inheritdoc />
    public DbContextRuleNode GetDbContextRules() {
        // pick the rule file that matches the context name
        var rules = GetRules();
        if (rules == null || rules.Count == 0) return new(DbContextRule.DefaultNoRulesFoundBehavior);
        var contextName = CodeGenOptions?.ContextName?.Trim();
        // ReSharper disable once InvertIf
        if (contextName.HasNonWhiteSpace()) {
            var rule = rules!.FirstOrDefault(o => o.DbName?.Trim().EqualsIgnoreCase(contextName) == true);
            if (rule != null) return rule;
        }

        return rules!.FirstOrDefault(o => !o.Rule.Schemas.IsNullOrEmpty()) ?? rules[0];
    }


    /// <inheritdoc />
    public IDesignTimeRuleLoader SetCodeGenerationOptions(ref ModelCodeGenerationOptions options) {
        var nativeOptions = System.Text.Json.JsonSerializer.Serialize(options);
        var optionsEx = System.Text.Json.JsonSerializer.Deserialize<ModelCodeGenerationOptionsEx>(nativeOptions);

        options = CodeGenOptions = optionsEx;
        if (logger != null) {
            var contextName = CodeGenOptions?.ContextName;
            if (contextName != null)
                logger.WriteVerbose($"EF Ruler notified that DB Context '{contextName}' will be reverse engineered.");
            else
                logger.WriteVerbose("EF Ruler was not given a target DB Context name.");
        }

        var projectDir = GetProjectDir();
        if (logger != null) {
            if (projectDir.HasNonWhiteSpace())
                logger.WriteVerbose($"EF Ruler identified project dir: {projectDir}");
            else
                logger.WriteVerbose("EF Ruler did not identify a project dir");
        }

        SolutionPath = projectDir?.FindSolutionParentPath();
        if (targetAssemblies?.Count > 0) {
            if (targetAssemblies.IsReadOnly) targetAssemblies = new List<TargetAssembly>();
            else targetAssemblies.Clear();
        }

        if (projectDir.IsNullOrWhiteSpace()) return this;
        if (Directory.GetCurrentDirectory() != projectDir) {
            // ensure the rules are reloaded if they are accessed again
            loadedRules = null;
        }

        var dir = new DirectoryInfo(projectDir!);
        if (!dir.Exists) return this;
        InitializeTargetAssemblies(options, projectDir);
        CodeGenOptions.SplitEntityTypeConfigurations = InitializeConfigurationSplitting(projectDir);
        InitializeOtherTemplates(projectDir);

        return this;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual void InitializeTargetAssemblies(ModelCodeGenerationOptions options, string projectDir) {
        try {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(o => new TargetAssembly(o)).ToArray();

            var csProj = projectDir.InspectProject();
            var assemblyName = csProj.GetAssemblyName().CoalesceWhiteSpace(options.RootNamespace);

            var targetAssembliesQuery = assemblies.Where(o => !o.Assembly.IsDynamic);

            // if assembly name is available, filter by it. otherwise, filter by the folder:
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (assemblyName.IsNullOrWhiteSpace())
                targetAssembliesQuery =
                    targetAssembliesQuery.Where(o => o.Assembly.Location.StartsWith(SolutionPath ?? projectDir));
            else
                targetAssembliesQuery =
                    targetAssembliesQuery.Where(o => o.AssemblyName.Name.EqualsIgnoreCase(assemblyName));

            targetAssemblies = targetAssembliesQuery.ToList();
            if (targetAssemblies.Count > 0)
                logger?.WriteInformation($"EF Ruler resolved target assembly: {targetAssemblies[0].Assembly.GetName().Name}");

            if (targetAssemblies.Count is <= 0 or > 2) return;
            // we have a small number of targets.  add the references of our targets to expand the list. better for type resolution later
            foreach (var targetAssembly in targetAssemblies.ToArray()) {
                var ans = targetAssembly.Assembly.GetReferencedAssemblies();
                foreach (var an in ans) {
                    // the assembly is lazy loaded later on if it is not already in memory
                    var loaded = assemblies.FirstOrDefault(o => o.AssemblyName.FullName == an.FullName) ??
                                 assemblies.FirstOrDefault(o => o.AssemblyName.Name == an.Name);
                    targetAssemblies.Add(loaded ?? new(an));
                }
            }
        } catch (Exception ex) {
            Console.WriteLine($"Assembly inspection failed: {ex.Message}");
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual bool InitializeConfigurationSplitting(string projectDir) {
        if (projectDir.IsNullOrWhiteSpace()) return false;
        if (!(EfVersion?.Major >= 7)) return false;
        // EF v7 supports entity config templating.
        var rules = GetDbContextRules();
        var splitConfigs = rules?.Rule?.SplitEntityTypeConfigurations ?? false;

        var configurationTemplate = RuledTemplatedModelGenerator.GetEntityTypeConfigurationFile(projectDir);
        if (configurationTemplate?.Directory == null) return false;

        if (splitConfigs) {
            if (configurationTemplate.Exists) return true;
            // we need to create the t4
            var assembly = GetType().Assembly;
            // var names = assembly.GetManifestResourceNames();
            // var entityTypeConfigurationName = names.FirstOrDefault(o => o.Contains("EntityTypeConfiguration"));
            // if (entityTypeConfigurationName == null) return;
            try {
                var text = assembly.GetResourceText("EntityFrameworkRuler.Design.Resources.EntityTypeConfiguration.t4");
                if (text.IsNullOrWhiteSpace()) return false;
                if (!configurationTemplate.Directory.FullName.EnsurePathExists()) return false; // could not create directory
                File.WriteAllText(configurationTemplate.FullName, text, Encoding.UTF8);
                return true;
            } catch (Exception ex) {
                logger?.WriteError($"Error generating EntityTypeConfiguration.t4 file: {ex.Message}");
                return false;
            }
        }

        if (!configurationTemplate.Exists) return false;
        // we need to remove the t4
        try {
            var bakFile = configurationTemplate.FullName + ".bak";
            if (File.Exists(bakFile)) {
                // just delete the t4
                File.Delete(configurationTemplate.FullName);
            } else {
                // rename the t4 to bak
                File.Move(configurationTemplate.FullName, bakFile);
            }
        } catch (Exception ex) {
            logger?.WriteError($"Error removing EntityTypeConfiguration.t4 file: {ex.Message}");
        }

        return false;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual void InitializeOtherTemplates(string projectDir) {
        if (projectDir.IsNullOrWhiteSpace()) return;
        if (!(EfVersion?.Major >= 7)) return;

        List<(string src, FileInfo tgt)> templates = new() {
            ("EntityFrameworkRuler.Design.Resources.Functions.t4", RuledTemplatedModelGenerator.GetFunctionFile(projectDir)),
            ("EntityFrameworkRuler.Design.Resources.DbContextFunctions.t4", RuledTemplatedModelGenerator.GetDbContextFunctionsFile(projectDir)),
            ("EntityFrameworkRuler.Design.Resources.FunctionsInterface.t4", RuledTemplatedModelGenerator.GetFunctionsInterfaceFile(projectDir)),
            ("EntityFrameworkRuler.Design.Resources.DbContextExtensions.t4", RuledTemplatedModelGenerator.GetDbContextExtensionsFile(projectDir)),
        };
        if (EfVersion?.Major >= 7) {
            templates.Add(("EntityFrameworkRuler.Design.Resources.DbContext.t4", RuledTemplatedModelGenerator.GetDbContextFile(projectDir)));
            templates.Add(("EntityFrameworkRuler.Design.Resources.EntityType.t4", RuledTemplatedModelGenerator.GetEntityTypeFile(projectDir)));
        }

        foreach (var (src, template) in templates) {
            if (template?.Directory == null) return;
#if !DEBUG || true
            if (template.Exists) return;
#endif

            // we need to create the t4
            var assembly = GetType().Assembly;
            try {
                var text = assembly.GetResourceText(src);
                if (text.IsNullOrWhiteSpace()) return;
                if (!template.Directory.FullName.EnsurePathExists()) return; // could not create directory
                File.WriteAllText(template.FullName, text, Encoding.UTF8);
            } catch (Exception ex) {
                logger?.WriteError($"Error generating {template.Name} file: {ex.Message}");
            }
        }
    }


    /// <inheritdoc />
    public IDesignTimeRuleLoader SetReverseEngineerOptions(ModelReverseEngineerOptions options) {
        ReverseEngineerOptions = options;
        return this;
    }


    /// <summary> Internal load method for all rules </summary>
    protected virtual IReadOnlyList<DbContextRuleNode> GetRules() {
        List<DbContextRuleNode> Fetch() {
            var nodes = new List<DbContextRuleNode>();
            var projectFolder = GetProjectDir();
            if (projectFolder.IsNullOrWhiteSpace() || !Directory.Exists(projectFolder)) {
                logger?.WriteWarning("Current project directory could not be determined for rule loading.");
            } else {
                var loadRulesResponse = ruleLoader.LoadRulesInProjectPath(new LoadOptions(projectFolder)).GetAwaiter().GetResult();
                logger?.WriteInformation($"EF Ruler loaded {loadRulesResponse.Rules?.Count ?? 0} rule file(s).");
                if (logger != null && loadRulesResponse.Rules?.Count > 0) {
                    foreach (var rule in loadRulesResponse.Rules) {
                        if (rule is not DbContextRule contextRules) continue;
                        logger.WriteVerbose(
                            $"DB Context Rules for {contextRules.Name} loaded with {contextRules.Schemas.Count} schemas and {contextRules.Schemas.SelectMany(o => o.Entities).Count()} tables from {(contextRules.FilePath?.Length > 0 ? Path.GetFileName(contextRules.FilePath) : string.Empty)}");
                        nodes.Add(new(contextRules));
                    }
                }
            }

            if (nodes.Count == 0) nodes.Add(new(DbContextRule.DefaultNoRulesFoundBehavior));

            return nodes;
        }

        loadedRules ??= Fetch();
        return loadedRules ?? new();
    }

    //private void LoaderLog(object sender, LogMessage msg) {
    //    switch (msg.Type) {
    //        case LogType.Warning:
    //            reporter?.WriteWarning(msg.Message);
    //            break;
    //        case LogType.Error:
    //            reporter?.WriteError(msg.Message);
    //            break;
    //        case LogType.Information:
    //        default:
    //            reporter?.WriteVerbosely(msg.Message);
    //            break;
    //    }
    //}

    /// <summary> Get the project base folder where the EF context model is being built </summary>
    public virtual string GetProjectDir() {
        // use reflecting to access ProjectDir property, which was added in EF 7.
        // otherwise, runtime binding errors may occur against EF 6.
        if (CodeGenOptions == null) return PathExtensions.FindProjectDirUnderCurrentCached()?.FullName;
        string folder = null;
        var prop = CodeGenOptions.GetType().GetProperty("ProjectDir");
        if (prop != null) folder = prop.GetValue(CodeGenOptions) as string;
        if (folder.IsNullOrWhiteSpace() && CodeGenOptions.ContextDir.HasNonWhiteSpace() && Path.IsPathRooted(CodeGenOptions.ContextDir))
            folder = CodeGenOptions.ContextDir.FindProjectParentPath();
        return folder.IsNullOrWhiteSpace() ? PathExtensions.FindProjectDirUnderCurrentCached()?.FullName : folder;
    }
}

internal sealed class TargetAssembly {
    private readonly Lazy<Assembly> assembly;
    private AssemblyName name;

    public TargetAssembly(AssemblyName name) {
        this.name = name;
        assembly = new(Factory, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public AssemblyName AssemblyName => name ??= assembly.Value?.GetName();
    public Assembly Assembly => assembly.Value;

    private Assembly Factory() {
        try {
            return Assembly.Load(name);
        } catch {
            return null;
        }
    }

    public TargetAssembly(Assembly assembly) {
        this.assembly = new(assembly);
    }
}