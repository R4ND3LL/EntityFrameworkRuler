using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Design.Services;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DesignTimeRuleLoader : IDesignTimeRuleLoader {
    private readonly IServiceProvider serviceProvider;
    private readonly IOperationReporter reporter;

    /// <summary> Creates the rule loader </summary>
    public DesignTimeRuleLoader(IServiceProvider serviceProvider, IOperationReporter reporter) {
        this.serviceProvider = serviceProvider;
        this.reporter = reporter;
        var reporterAssembly = reporter.GetType().Assembly;
        var assemblyName = reporterAssembly?.GetName();
        EfVersion = assemblyName?.Version;
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (EfVersion != null) reporter?.WriteInformation($"EF Ruler detected Entity Framework v{assemblyName.Version}");
        else reporter?.WriteInformation("EF Ruler could not detect the EF version");
    }

    /// <summary> The detected entity framework version.  </summary>
    public Version EfVersion { get; protected set; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected bool IsEf6 => EfVersion?.Major == 6;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected bool IsEf7 => EfVersion?.Major == 7;

    private LoadRulesResponse response;

    /// <inheritdoc />
    public ModelCodeGenerationOptions CodeGenOptions { get; private set; }

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
    public DbContextRule GetDbContextRules() {
        // pick the rule file that matches the context name
        var rules = GetRules()?.OfType<DbContextRule>().ToArray();
        if (rules.IsNullOrEmpty()) return DbContextRule.DefaultNoRulesFoundBehavior;
        var contextName = CodeGenOptions?.ContextName?.Trim();
        // ReSharper disable once InvertIf
        if (contextName.HasNonWhiteSpace()) {
            var rule = rules!.FirstOrDefault(o => o.Name?.Trim().EqualsIgnoreCase(contextName) == true);
            if (rule != null) return rule;
        }

        return rules!.FirstOrDefault(o => !o.Schemas.IsNullOrEmpty()) ?? rules[0];
    }


    /// <inheritdoc />
    public IDesignTimeRuleLoader SetCodeGenerationOptions(ModelCodeGenerationOptions options) {
        CodeGenOptions = options;

        var projectDir = GetProjectDir();
        SolutionPath = projectDir?.FindSolutionParentPath();
        if (targetAssemblies?.Count > 0) {
            if (targetAssemblies.IsReadOnly) targetAssemblies = new List<TargetAssembly>();
            else targetAssemblies.Clear();
        }

        if (projectDir.IsNullOrWhiteSpace()) return this;
        if (Directory.GetCurrentDirectory() != projectDir) {
            // ensure the rules are reloaded if they are accessed again
            response = null;
        }

        var dir = new DirectoryInfo(projectDir!);
        if (!dir.Exists) return this;
        InitializeTargetAssemblies(options, projectDir);
        InitializeConfigurationSplitting(projectDir);
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
                reporter?.WriteInformation($"Rule loader resolved target assembly: {targetAssemblies[0].Assembly.GetName().Name}");

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
    public virtual void InitializeConfigurationSplitting(string projectDir) {
        if (projectDir.IsNullOrWhiteSpace()) return;
        if (!(EfVersion?.Major >= 7)) return;
        // EF v7 supports entity config templating.
        var rules = GetDbContextRules();
        var splitConfigs = rules?.SplitEntityTypeConfigurations ?? false;

        var configurationTemplate = RuledTemplatedModelGenerator.GetEntityTypeConfigurationFile(projectDir);
        if (configurationTemplate?.Directory == null) return;

        if (splitConfigs) {
            if (configurationTemplate.Exists) return;
            // we need to create the t4
            var assembly = GetType().Assembly;
            // var names = assembly.GetManifestResourceNames();
            // var entityTypeConfigurationName = names.FirstOrDefault(o => o.Contains("EntityTypeConfiguration"));
            // if (entityTypeConfigurationName == null) return;
            try {
                var text = assembly.GetResourceText("EntityFrameworkRuler.Design.Resources.EntityTypeConfiguration.t4");
                if (text.IsNullOrWhiteSpace()) return;
                if (!configurationTemplate.Directory.FullName.EnsurePathExists()) return; // could not create directory
                File.WriteAllText(configurationTemplate.FullName, text, Encoding.UTF8);
            } catch (Exception ex) {
                reporter?.WriteError($"Error generating EntityTypeConfiguration.t4 file: {ex.Message}");
            }
        } else {
            if (!configurationTemplate.Exists) return;
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
                reporter?.WriteError($"Error removing EntityTypeConfiguration.t4 file: {ex.Message}");
            }
        }
    }


    /// <inheritdoc />
    public IDesignTimeRuleLoader SetReverseEngineerOptions(ModelReverseEngineerOptions options) {
        ReverseEngineerOptions = options;
        return this;
    }


    /// <summary> Internal load method for all rules </summary>
    protected virtual IEnumerable<IRuleModelRoot> GetRules() {
        LoadRulesResponse Fetch() {
            var projectFolder = GetProjectDir();
            if (projectFolder.IsNullOrWhiteSpace() || !Directory.Exists(projectFolder)) {
                reporter?.WriteWarning("Current project directory could not be determined for rule loading.");
                return null;
            }

            var loader = new RuleLoader(projectFolder);
            loader.OnLog += LoaderOnLog;
            var loadRulesResponse = loader.LoadRulesInProjectPath().GetAwaiter().GetResult();
            loader.OnLog -= LoaderOnLog;
            reporter?.WriteInformation($"EF Ruler loaded {loadRulesResponse.Rules?.Count ?? 0} rule file(s).");
            return loadRulesResponse;
        }

        response ??= Fetch() ?? new LoadRulesResponse();
        return response?.Rules ?? Enumerable.Empty<IRuleModelRoot>();
    }

    private void LoaderOnLog(object sender, LogMessage msg) {
        switch (msg.Type) {
            case LogType.Warning:
                reporter?.WriteWarning(msg.Message);
                break;
            case LogType.Error:
                reporter?.WriteError(msg.Message);
                break;
            case LogType.Information:
            default:
                reporter?.WriteVerbosely(msg.Message);
                break;
        }
    }

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