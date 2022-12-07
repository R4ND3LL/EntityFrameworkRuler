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
    public RuleSaver() : this(null, null) { }

    /// <summary> Create rule Saver for making changes to project files </summary>
    [ActivatorUtilitiesConstructor]
    public RuleSaver(IRuleSerializer serializer, IMessageLogger logger) : base(logger) {
        Serializer = serializer;
    }


    #region properties

    /// <summary> The rule serialize to use while saving </summary>
    public IRuleSerializer Serializer { get; set; }

    #endregion

    /// <inheritdoc />
    public Task<SaveRulesResponse> SaveRules(IRuleModelRoot rule, string projectBasePath, string dbContextRulesFile = null) {
        return SaveRules(new SaveOptions(projectBasePath: projectBasePath, dbContextRulesFile: dbContextRulesFile, rules: rule));
    }

    /// <inheritdoc />
    public Task<SaveRulesResponse> SaveRules(string projectBasePath, string dbContextRulesFile, IRuleModelRoot rule,
        params IRuleModelRoot[] rules) {
        var options = new SaveOptions(projectBasePath: projectBasePath, dbContextRulesFile: dbContextRulesFile);
        if (rule != null) options.Rules.Add(rule);
        if (rules != null) options.Rules.AddRange(rules);
        return SaveRules(options);
    }

    /// <inheritdoc />
    public async Task<SaveRulesResponse> SaveRules(SaveOptions request) {
        var response = new SaveRulesResponse(Logger);
        response.Log += OnResponseLog;
        try {
            if (request == null) {
                response.GetInternals().LogError("Save options are null");
                return response;
            }

            var dir = request.ProjectBasePath.HasNonWhiteSpace() ? new DirectoryInfo(request.ProjectBasePath) : null;
            if (dir?.Exists != true) {
                response.GetInternals().LogError("Output folder does not exist");
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
                        fileName = fileName.ApplyContextNameMask(name);
                        var path = await WriteRules(rulesRoot, rulesRoot.GetFilePath().CoalesceWhiteSpace(fileName));
                        response.SavedRules.Add(path);
                        response.GetInternals().LogInformation($"{rulesRoot.Kind} rule file written to {fileName}");
                    }
                } catch (Exception ex) {
                    response.GetInternals().LogError($"Error writing rule to file {fileName}: {ex.Message}");
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
    /// <inheritdoc />
    public SaveRulesResponse(IMessageLogger logger) : base(logger) { }

    /// <summary> List of file paths to the rules that were saved. </summary>
    public List<string> SavedRules { get; } = new();

    /// <inheritdoc />
    public override bool Success => base.Success && SavedRules.Count > 0;


}