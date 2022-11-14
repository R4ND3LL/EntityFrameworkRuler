using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkRuler.Common;
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
    }

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
    protected virtual List<IEdmxRuleModelRoot> GetRules() {
        LoadRulesResponse Fetch() {
            var projectFolder = GetProjectDir();
            if (projectFolder.IsNullOrWhiteSpace() || !Directory.Exists(projectFolder)) {
                WriteWarning("Current project directory could not be determined for rule loading.");
                return null;
            }

            var loader = new RuleLoader(projectFolder);
            loader.OnLog += LoaderOnLog;
            var loadRulesResponse = loader.LoadRulesInProjectPath().GetAwaiter().GetResult();
            loader.OnLog -= LoaderOnLog;
            return loadRulesResponse;
        }

        response ??= Fetch() ?? new LoadRulesResponse();
        return response?.Rules;
    }

    private void LoaderOnLog(object sender, LogMessage msg) {
        switch (msg.Type) {
            case LogType.Warning:
                WriteWarning(msg.Message);
                break;
            case LogType.Error:
                WriteError(msg.Message);
                break;
            case LogType.Information:
            default:
                WriteInformation(msg.Message);
                break;
        }
    }

    /// <summary> Get the project folder where the EF context model is being built </summary>
    protected virtual string GetProjectDir() {
#if NET6
        return CodeGenOptions?.ContextDir?.FindProjectParentPath() ?? Directory.GetCurrentDirectory();
#elif NET7
        return CodeGenOptions?.ProjectDir ?? CodeGenOptions?.ContextDir?.FindProjectParentPath() ?? Directory.GetCurrentDirectory();
#endif
    }

    internal void WriteError(string msg) {
        reporter?.WriteError(msg);
        DebugLog(msg);
    }

    internal void WriteWarning(string msg) {
        reporter?.WriteWarning(msg);
        DebugLog(msg);
    }

    internal void WriteInformation(string msg) {
        reporter?.WriteInformation(msg);
        DebugLog(msg);
    }

    internal void WriteVerbose(string msg) {
        reporter?.WriteVerbose(msg);
        DebugLog(msg);
    }

    [Conditional("DEBUG")]
    internal static void DebugLog(string msg) => Console.WriteLine(msg);
}