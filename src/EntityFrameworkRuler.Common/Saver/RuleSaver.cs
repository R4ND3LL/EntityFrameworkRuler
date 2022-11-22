using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Saver;

/// <summary> EF Rule Saver </summary>
public class RuleSaver : RuleProcessor, IRuleSaver {
    /// <summary> Create rule Saver for making changes to project files </summary>
    /// <param name="projectBasePath">project folder containing rules and target files.</param>
    public RuleSaver(string projectBasePath)
        : this(new SaveOptions() {
            ProjectBasePath = projectBasePath
        }) {
    }

    /// <summary> Create rule Saver for making changes to project files </summary>
    [ActivatorUtilitiesConstructor]
    public RuleSaver(SaveOptions options) {
        Options = options ?? new SaveOptions() { ProjectBasePath = Directory.GetCurrentDirectory() };
    }


    #region properties

    /// <inheritdoc />
    public SaveOptions Options { get; }

    /// <summary> The target project path containing entity models. </summary>
    public string ProjectBasePath {
        get => Options.ProjectBasePath;
        set => Options.ProjectBasePath = value;
    }

    #endregion

    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="rule"> The rule model to save. </param>
    /// <param name="projectBasePath"> The location to save the rule files. </param>
    /// <param name="fileNameOptions"> Custom naming options for the rule files.  Optional. This parameter can be used to skip writing a rule file by setting that rule file to null. </param>
    /// <returns> True if completed with no errors.  When false, see Errors collection for details. </returns>
    /// <exception cref="Exception"></exception>
    public Task<SaveRulesResponse> TrySaveRules(IRuleModelRoot rule, string projectBasePath,
        RuleFileNameOptions fileNameOptions = null) => TrySaveRules(new[] { rule }, projectBasePath, fileNameOptions);

    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="rules"> The rule models to save. </param>
    /// <param name="projectBasePath"> The location to save the rule files. </param>
    /// <param name="fileNameOptions"> Custom naming options for the rule files.  Optional. This parameter can be used to skip writing a rule file by setting that rule file to null. </param>
    /// <returns> True if completed with no errors.  When false, see Errors collection for details. </returns>
    /// <exception cref="Exception"></exception>
    public async Task<SaveRulesResponse> TrySaveRules(IEnumerable<IRuleModelRoot> rules, string projectBasePath,
        RuleFileNameOptions fileNameOptions = null) {
        var response = new SaveRulesResponse();
        response.OnLog += ResponseOnLog;
        try {
            var dir = new DirectoryInfo(projectBasePath);
            if (!dir.Exists) {
                response.LogError("Output folder does not exist");
                return response;
            }

            fileNameOptions ??= new();

            await TryWriteRules<DbContextRule>(
                fileNameOptions.DbContextRulesFile.CoalesceWhiteSpace(() => new RuleFileNameOptions().DbContextRulesFile));

            return response;


            async Task TryWriteRules<T>(string fileName) where T : class, IRuleModelRoot {
                try {
                    if (fileName.IsNullOrWhiteSpace()) return; // file skipped by user
                    foreach (var rulesRoot in rules?.OfType<T>()) {
                        var name = rulesRoot.GetFinalName().NullIfWhitespace() ?? "dbcontext";
                        fileName = fileName.Replace("<ContextName>", name, StringComparison.OrdinalIgnoreCase);
                        var path = await WriteRules<T>(rulesRoot, fileName);
                        response.SavedRules.Add(path);
                        response.LogInformation($"{rulesRoot.Kind} rule file written to {fileName}");
                    }
                } catch (Exception ex) {
                    response.LogError($"Error writing rule to file {fileName}: {ex.Message}");
                }
            }

            async Task<string> WriteRules<T>(T rulesRoot, string filename)
                where T : class, IRuleModelRoot {
                var path = Path.IsPathRooted(filename) ? filename : Path.Combine(dir.FullName, filename);
                await rulesRoot.ToJson<T>(path);
                return path;
            }
        } finally {
            response.OnLog -= ResponseOnLog;
        }
    }
}

/// <summary> Response for save rules operation </summary>
public sealed class SaveRulesResponse : LoggedResponse {
    /// <summary> List of file paths to the rules that were saved. </summary>
    public List<string> SavedRules { get; } = new();
}