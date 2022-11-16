using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Rules.NavigationNaming;
using EntityFrameworkRuler.Rules.PrimitiveNaming;
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
    public Version EfVersion { get; set; }

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


    private IList<Assembly> targetAssemblies;


    /// <inheritdoc />
    public IList<Assembly> TargetAssemblies {
        get => targetAssemblies ?? Array.Empty<Assembly>();
        private set => targetAssemblies = value;
    }

    /// <inheritdoc />
    public PrimitiveNamingRules GetPrimitiveNamingRules() {
        return GetRules().OfType<PrimitiveNamingRules>().FirstOrDefault();
    }

    /// <inheritdoc />
    public NavigationNamingRules GetNavigationNamingRules() {
        return GetRules().OfType<NavigationNamingRules>().FirstOrDefault();
    }

    /// <inheritdoc />
    public IDesignTimeRuleLoader SetCodeGenerationOptions(ModelCodeGenerationOptions options) {
        CodeGenOptions = options;

        var projectDir = GetProjectDir();
        SolutionPath = projectDir?.FindSolutionParentPath();
        if (TargetAssemblies?.Count > 0) {
            if (TargetAssemblies.IsReadOnly) TargetAssemblies = new List<Assembly>();
            else TargetAssemblies.Clear();
        }

        if (projectDir.IsNullOrWhiteSpace()) return this;
        if (Directory.GetCurrentDirectory() != projectDir) {
            // ensure the rules are reloaded if they are accessed again
            response = null;
        }

        var dir = new DirectoryInfo(projectDir!);
        if (!dir.Exists) return this;
        try {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var csProj = projectDir.InspectProject();
            var assemblyName = csProj.GetAssemblyName().CoalesceWhiteSpace(options.RootNamespace);

            var targetAssembliesQuery = assemblies.Where(o => !o.IsDynamic);

            // if assembly name is available, filter by it. otherwise, filter by the folder:
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (assemblyName.IsNullOrWhiteSpace())
                targetAssembliesQuery = targetAssembliesQuery.Where(o => o.Location.StartsWith(projectDir));
            else
                targetAssembliesQuery = targetAssembliesQuery.Where(o => o.GetName().Name == assemblyName);

            TargetAssemblies = targetAssembliesQuery.ToList();
            if (TargetAssemblies.Count > 0) {
                reporter?.WriteInformation($"Rule loader resolved target assembly: {TargetAssemblies[0].GetName().Name}");
            }
            // if (TargetAssemblies.Count > 0) {
            //     // also add direct references that are under the sln folder as locations to load property types from
            //     if (SolutionPath.HasNonWhiteSpace()) {
            //         foreach (var targetAssembly in TargetAssemblies) {
            //             var ans = targetAssembly.GetReferencedAssemblies();
            //             foreach (var an in ans) {
            //             }
            //         }
            //     }
            // }
        } catch (Exception ex) {
            Console.WriteLine($"Assembly inspection failed: {ex.Message}");
        }

        return this;
    }

    /// <inheritdoc />
    public IDesignTimeRuleLoader SetReverseEngineerOptions(ModelReverseEngineerOptions options) {
        ReverseEngineerOptions = options;
        return this;
    }


    /// <summary> Internal load method for all rules </summary>
    protected virtual List<IRuleModelRoot> GetRules() {
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
        return response?.Rules;
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

    /// <summary> Get the project folder where the EF context model is being built </summary>
    protected virtual string GetProjectDir() {
        // use reflecting to access ProjectDir property, which was added in EF 7.
        // otherwise, binding errors may occur against EF 6.
        if (CodeGenOptions == null) return Directory.GetCurrentDirectory();
        string folder = null;
        var prop = CodeGenOptions.GetType().GetProperty("ProjectDir");
        if (prop != null) folder = prop.GetValue(CodeGenOptions) as string;
        if (folder.IsNullOrWhiteSpace() && CodeGenOptions.ContextDir.HasNonWhiteSpace() && Path.IsPathRooted(CodeGenOptions.ContextDir))
            folder = CodeGenOptions.ContextDir.FindProjectParentPath();
        return folder.IsNullOrWhiteSpace() ? Directory.GetCurrentDirectory() : folder;
    }
}