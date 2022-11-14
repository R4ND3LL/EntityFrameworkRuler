using System.Reflection;
using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Rules.NavigationNaming;
using EntityFrameworkRuler.Rules.PrimitiveNaming;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Design.Services;

/// <inheritdoc />
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultRuleLoader : IRuleLoader {
    private readonly IServiceProvider serviceProvider;

    /// <summary> Creates the rule loader </summary>
    public DefaultRuleLoader(IServiceProvider serviceProvider) { this.serviceProvider = serviceProvider; }

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

    // /// <inheritdoc />
    // public PropertyTypeChangingRules GetPropertyTypeChangingRules() {
    //     return GetRules().OfType<PropertyTypeChangingRules>().FirstOrDefault();
    // }

    /// <inheritdoc />
    public NavigationNamingRules GetNavigationNamingRules() {
        return GetRules().OfType<NavigationNamingRules>().FirstOrDefault();
    }

    /// <inheritdoc />
    public IRuleLoader SetCodeGenerationOptions(ModelCodeGenerationOptions options) {
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

        var dir = new DirectoryInfo(projectDir);
        if (!dir.Exists) return this;
        try {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var csProj = RuleApplicator.InspectProject(projectDir, null);
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
    public IRuleLoader SetReverseEngineerOptions(ModelReverseEngineerOptions options) {
        ReverseEngineerOptions = options;
        return this;
    }


    /// <summary> Internal load method for all rules </summary>
    protected virtual List<IEdmxRuleModelRoot> GetRules() {
        LoadRulesResponse Fetch() {
            var projectFolder = GetProjectDir();
            if (projectFolder.IsNullOrWhiteSpace() || !Directory.Exists(projectFolder)) return null;
            var applicator = new RuleApplicator(projectFolder);
            var loadRulesResponse = applicator.LoadRulesInProjectPath().GetAwaiter().GetResult();
            return loadRulesResponse;
        }

        response ??= Fetch();
        return response.Rules;
    }

    /// <summary> Get the project folder where the EF context model is being built </summary>
    protected virtual string GetProjectDir() {
#if NET6
        return CodeGenOptions?.ContextDir?.FindProjectParentPath() ?? Directory.GetCurrentDirectory();
#elif NET7
        return CodeGenOptions?.ProjectDir ?? CodeGenOptions?.ContextDir?.FindProjectParentPath() ?? Directory.GetCurrentDirectory();
#endif
    }
}