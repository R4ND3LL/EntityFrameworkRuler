using System.Reflection;
using EdmxRuler.Applicator;
using EdmxRuler.Applicator.CsProjParser;
using EdmxRuler.Extensions;
using EdmxRuler.Rules;
using EdmxRuler.Rules.NavigationNaming;
using EdmxRuler.Rules.PrimitiveNaming;
using EdmxRuler.Rules.PropertyTypeChanging;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Services;

/// <inheritdoc />
public class DefaultRuleLoader : IRuleLoader {
    private readonly IServiceProvider serviceProvider;

    /// <summary> Creates the rule loader </summary>
    public DefaultRuleLoader(IServiceProvider serviceProvider) { this.serviceProvider = serviceProvider; }

    private LoadRulesResponse response;
    public ModelCodeGenerationOptions CodeGenOptions { get; private set; }
    private IList<Assembly> targetAssemblies;

    public IList<Assembly> TargetAssemblies {
        get => targetAssemblies ?? Array.Empty<Assembly>();
        private set => targetAssemblies = value;
    }

    /// <inheritdoc />
    public PrimitiveNamingRules GetPrimitiveNamingRules() {
        return GetRules().OfType<PrimitiveNamingRules>().FirstOrDefault();
    }

    /// <inheritdoc />
    public PropertyTypeChangingRules GetPropertyTypeChangingRules() {
        return GetRules().OfType<PropertyTypeChangingRules>().FirstOrDefault();
    }

    /// <inheritdoc />
    public NavigationNamingRules GetNavigationNamingRules() {
        return GetRules().OfType<NavigationNamingRules>().FirstOrDefault();
    }

    /// <inheritdoc />
    public IRuleLoader SetCodeGenerationOptions(ModelCodeGenerationOptions options) {
        this.CodeGenOptions = options;
        if (Directory.GetCurrentDirectory() != options.ProjectDir) {
            // ensure the rules are reloaded if they are accessed again
            response = null;
        }

        var dir = new DirectoryInfo(options.ProjectDir);
        if (dir.Exists) {
            try {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var csProj = RuleApplicator.InspectProject(options.ProjectDir, null);
                var assemblyName = csProj.GetAssemblyName().CoalesceWhiteSpace(options.RootNamespace);

                var targetAssembliesQuery = assemblies.Where(o => !o.IsDynamic);

                // if assembly name is available, filter by it. otherwise, filter by the folder:
                if (assemblyName.IsNullOrWhiteSpace())
                    targetAssembliesQuery = targetAssembliesQuery.Where(o => o.Location.StartsWith(options.ProjectDir));
                else
                    targetAssembliesQuery = targetAssembliesQuery.Where(o => o.GetName().Name == assemblyName);

                TargetAssemblies = targetAssembliesQuery.ToList();

                if (TargetAssemblies.Count > 0) {
                    // also add direct references that are under the sln folder
                    var slnPath = csProj.FindSolutionParentPath();
                    if (slnPath.HasNonWhiteSpace()) {
                        foreach (var targetAssembly in TargetAssemblies) {
                            var ans = targetAssembly.GetReferencedAssemblies();
                            foreach (var an in ans) {
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Assembly inspection failed: {ex.Message}");
            }
        }

        return this;
    }


    /// <summary> Internal load method for all rules </summary>
    protected virtual List<IEdmxRuleModelRoot> GetRules() {
        LoadRulesResponse Fetch() {
            var projectFolder = CodeGenOptions?.ProjectDir ?? Directory.GetCurrentDirectory();
            if (projectFolder.IsNullOrWhiteSpace() || !Directory.Exists(projectFolder)) return null;
            var applicator = new RuleApplicator(projectFolder);
            var loadRulesResponse = applicator.LoadRulesInProjectPath().GetAwaiter().GetResult();
            return loadRulesResponse;
        }

        response ??= Fetch();
        return response.Rules;
    }
}