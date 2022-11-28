using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
[assembly: CLSCompliant(false)]

namespace EntityFrameworkRuler.Loader;

/// <summary> EF Rule loader </summary>
public class RuleLoader : RuleHandler, IRuleLoader {
    /// <summary> Create rule Loader for making changes to project files </summary>
    public RuleLoader() : this(null) { }

    /// <summary> Create rule Loader for making changes to project files </summary>
    [ActivatorUtilitiesConstructor]
    public RuleLoader(IRuleSerializer serializer) {
        Serializer = serializer;
    }


    #region properties

    /// <summary> The rule serialize to use while loading </summary>
    public IRuleSerializer Serializer { get; set; }

    #endregion


    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="request"> The load request options. </param>
    /// <returns> Response with loaded rules and list of errors (if any). </returns>
    public async Task<LoadRulesResponse> LoadRulesInProjectPath(ILoadOptions request) {
        var response = new LoadRulesResponse();
        response.OnLog += OnResponseLog;
        var rules = response.Rules;
        try {
            var path = request?.ProjectBasePath;
            if (request?.ProjectBasePath == null) throw new ArgumentException("Invalid path", nameof(request));

            FileInfo[] jsonFiles = null;
            if (path.EndsWithIgnoreCase(".json")) {
                var f = new FileInfo(path);
                if (f.Exists) {
                    path = f.Directory?.FullName ?? path;
                    jsonFiles = new[] { f };
                } else throw new ArgumentException("Invalid path", nameof(request));
            }

            if (jsonFiles.IsNullOrEmpty()) {
                // locate all rule files in folder
                var nameMask = request.DbContextRulesFile.CoalesceWhiteSpace(() => new LoadOptions().DbContextRulesFile);
                if (nameMask.IsNullOrWhiteSpace()) return response;

                var csProjFile = path.FindProjectFileCached();
                if (csProjFile == null) throw new ArgumentException("csproj not found in target path", nameof(request));

                path = csProjFile.Directory?.FullName ?? path;

                var mask = request.DbContextRulesFile.Replace("<ContextName>", "*", StringComparison.OrdinalIgnoreCase);

                jsonFiles = path.FindFiles(mask, true, 2).ToArray();
                if (jsonFiles.Length == 0) return response; // nothing to do
            }

            var serializer = Serializer ?? new JsonRuleSerializer();

            if (!(jsonFiles?.Length > 0)) return response;
            foreach (var fileInfo in jsonFiles)
                try {
                    if (!fileInfo.Exists) continue;

                    if (await TryReadRules<DbContextRule>(fileInfo, response, serializer) is { } dbContextRule) {
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
            response.OnLog -= OnResponseLog;
        }
    }

    private static async Task<T> TryReadRules<T>(FileInfo jsonFile, LoggedResponse loggedResponse, IRuleSerializer ruleSerializer)
        where T : class, new() {
        var rules = await ruleSerializer.TryDeserializeFile<T>(jsonFile.FullName);
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