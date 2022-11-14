using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Rules.NavigationNaming;
using EntityFrameworkRuler.Rules.PrimitiveNaming;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
[assembly: CLSCompliant(false)]

namespace EntityFrameworkRuler.Loader;

public class RuleLoader : RuleProcessor, IRuleLoader {
    /// <summary> Create rule Loader for making changes to project files </summary>
    /// <param name="projectBasePath">project folder containing rules and target files.</param>
    public RuleLoader(string projectBasePath )
        : this(new LoaderOptions() {
            ProjectBasePath = projectBasePath
        }) {
    }

    /// <summary> Create rule Loader for making changes to project files </summary>
    [ActivatorUtilitiesConstructor]
    public RuleLoader(LoaderOptions options) {
        Options = options;
    }


    #region properties

    public LoaderOptions Options { get; }

    /// <summary> The target project path containing entity models. </summary>
    public string ProjectBasePath {
        get => Options.ProjectBasePath;
        set => Options.ProjectBasePath = value;
    }


    #endregion


    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="fileNameOptions"> optional rule file naming options </param>
    /// <returns> Response with loaded rules and list of errors. </returns>
    public async Task<LoadRulesResponse> LoadRulesInProjectPath(RuleFileNameOptions fileNameOptions = null) {
        var response = new LoadRulesResponse();
        response.OnLog += ResponseOnLog;
        var rules = response.Rules;
        try {
            if (ProjectBasePath == null) throw new ArgumentException(nameof(ProjectBasePath));

            var projectBasePath = ProjectBasePath;
            var csProjFiles = PathExtensions.ResolveCsProjFiles(ref projectBasePath);
            if (csProjFiles.IsNullOrEmpty()) throw new ArgumentException(nameof(ProjectBasePath));

            var fullProjectPath = csProjFiles.FirstOrDefault();
            if (fullProjectPath == null) throw new ArgumentException("csproj not found", nameof(ProjectBasePath));

            fileNameOptions ??= new();

            var jsonFiles = new[] { fileNameOptions.PrimitiveRulesFile, fileNameOptions.NavigationRulesFile }
                .Where(o => o.HasNonWhiteSpace())
                .Select(o => o.Trim())
                .ToArray();


            if (jsonFiles.Length == 0) return response; // nothing to do

            foreach (var jsonFile in jsonFiles)
                try {
                    if (jsonFile.IsNullOrWhiteSpace()) continue;
                    var fullPath = Path.Combine(projectBasePath, jsonFile);
                    var fileInfo = new FileInfo(fullPath);
                    if (!fileInfo.Exists) continue;

                    if (jsonFile == fileNameOptions.PrimitiveRulesFile) {
                        if (await TryReadRules<PrimitiveNamingRules>(fileInfo, response) is { } schemas)
                            rules.Add(schemas);
                    } else if (jsonFile == fileNameOptions.NavigationRulesFile) {
                        if (await TryReadRules<NavigationNamingRules>(fileInfo, response) is { } propertyRenamingRoot)
                            rules.Add(propertyRenamingRoot);
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    response.LogError($"Error processing {jsonFile}: {ex.Message}");
                }

            return response;
        } catch (Exception ex) {
            response.LogError($"Error: {ex.Message}");
            return response;
        } finally {
            response.OnLog -= ResponseOnLog;
        }
    }

    private static async Task<T> TryReadRules<T>(FileInfo jsonFile, LoggedResponse loggedResponse)
        where T : class, new() {
        var rules = await jsonFile.FullName.TryReadJsonFile<T>();
        if (rules != null) return rules;
        loggedResponse.LogError($"Unable to open {jsonFile.Name}");
        return null;
    }


}

public sealed class LoadRulesResponse : LoggedResponse {
    public List<IEdmxRuleModelRoot> Rules { get; } = new();
}

