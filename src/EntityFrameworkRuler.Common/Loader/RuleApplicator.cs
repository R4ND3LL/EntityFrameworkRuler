using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
[assembly: CLSCompliant(false)]

namespace EntityFrameworkRuler.Loader;

/// <summary> EF Rule loader </summary>
public class RuleLoader : RuleProcessor, IRuleLoader {
    /// <summary> Create rule Loader for making changes to project files </summary>
    /// <param name="projectBasePath">project folder containing rules and target files.</param>
    public RuleLoader(string projectBasePath)
        : this(new LoaderOptions() {
            ProjectBasePath = projectBasePath
        }) {
    }

    /// <summary> Create rule Loader for making changes to project files </summary>
    [ActivatorUtilitiesConstructor]
    public RuleLoader(LoaderOptions options) {
        Options = options ?? new LoaderOptions() { ProjectBasePath = Directory.GetCurrentDirectory() };
    }


    #region properties

    /// <inheritdoc />
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
            var csProjFile = projectBasePath.FindProjectFileCached();
            if (csProjFile == null) throw new ArgumentException(nameof(ProjectBasePath));

            projectBasePath = ProjectBasePath = csProjFile.Directory?.FullName ?? projectBasePath;
            var fullProjectPath = csProjFile.FullName;
            if (fullProjectPath == null) throw new ArgumentException("csproj not found", nameof(ProjectBasePath));

            fileNameOptions ??= new();

            if (fileNameOptions.DbContextRulesFile.IsNullOrWhiteSpace())
                return response;

            var mask = fileNameOptions.DbContextRulesFile.Replace("<ContextName>", "*", StringComparison.OrdinalIgnoreCase);

            var jsonFiles = projectBasePath.FindFiles(mask, true, 2).ToArray();
            if (jsonFiles.Length == 0) return response; // nothing to do

            foreach (var fileInfo in jsonFiles)
                try {
                    if (!fileInfo.Exists) continue;

                    if (await TryReadRules<DbContextRule>(fileInfo, response) is { } dbContextRule) {
                        dbContextRule.FilePath = fileInfo.FullName;
                        if (dbContextRule.Schemas == null) continue;
                        rules.Add(dbContextRule);
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    response.LogError($"Error processing {fileInfo.Name}: {ex.Message}");
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

/// <summary> Response for load rules operation </summary>
public sealed class LoadRulesResponse : LoggedResponse {
    /// <summary> The loaded rules </summary>
    public List<IRuleModelRoot> Rules { get; } = new();
}