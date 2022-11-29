using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Saver;

/// <summary> EF Rule Saver </summary>
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public class RuleSaver : RuleHandler, IRuleSaver {
    /// <summary> Create rule Saver for making changes to project files </summary>
    public RuleSaver() : this(null) { }

    /// <summary> Create rule Saver for making changes to project files </summary>
    [ActivatorUtilitiesConstructor]
    public RuleSaver(IRuleSerializer serializer) {
        Serializer = serializer;
    }


    #region properties

    /// <summary> The rule serialize to use while saving </summary>
    public IRuleSerializer Serializer { get; set; }

    #endregion

    /// <inheritdoc />
    public Task<SaveRulesResponse> SaveRules(string projectBasePath, string dbContextRulesFile = null, params IRuleModelRoot[] rules) {
        return SaveRules(new SaveOptions(projectBasePath: projectBasePath, dbContextRulesFile: dbContextRulesFile, rules: rules));
    }

    /// <inheritdoc />
    public async Task<SaveRulesResponse> SaveRules(SaveOptions request) {
        var response = new SaveRulesResponse();
        response.Log += OnResponseLog;
        try {
            if (request == null) {
                response.LogError("Save options are null");
                return response;
            }

            var dir = request.ProjectBasePath.HasNonWhiteSpace() ? new DirectoryInfo(request.ProjectBasePath) : null;
            if (dir?.Exists != true) {
                response.LogError("Output folder does not exist");
                return response;
            }

            var serializer = Serializer ?? new JsonRuleSerializer();

            if (request.Rules != null)
                await TryWriteRules<DbContextRule>(
                    request.DbContextRulesFile.CoalesceWhiteSpace(() => new SaveOptions().DbContextRulesFile));

            return response;


            async Task TryWriteRules<T>(string fileName) where T : class, IRuleModelRoot {
                try {
                    if (fileName.IsNullOrWhiteSpace()) return; // file skipped by user
                    foreach (var rulesRoot in request.Rules.OfType<T>()) {
                        var name = rulesRoot.GetFinalName().NullIfWhitespace() ?? "dbcontext";
                        fileName = fileName.Replace("<ContextName>", name, StringComparison.OrdinalIgnoreCase);
                        var path = await WriteRules(rulesRoot, rulesRoot.GetFilePath().CoalesceWhiteSpace(fileName));
                        response.SavedRules.Add(path);
                        response.LogInformation($"{rulesRoot.Kind} rule file written to {fileName}");
                    }
                } catch (Exception ex) {
                    response.LogError($"Error writing rule to file {fileName}: {ex.Message}");
                }
            }

            async Task<string> WriteRules<T>(T rulesRoot, string filename)
                where T : class, IRuleModelRoot {
                var path = Path.IsPathRooted(filename) ? filename : Path.Combine(dir.FullName, filename!);
                await serializer.Serialize(rulesRoot, path);
                return path;
            }
        } finally {
            response.Log -= OnResponseLog;
        }
    }
}

/// <summary> Response for save rules operation </summary>
public sealed class SaveRulesResponse : LoggedResponse {
    /// <summary> List of file paths to the rules that were saved. </summary>
    public List<string> SavedRules { get; } = new();
}