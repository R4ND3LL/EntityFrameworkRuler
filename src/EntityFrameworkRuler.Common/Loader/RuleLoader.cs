﻿using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Loader;

/// <summary> EF Rule loader </summary>
public class RuleLoader : RuleHandler, IRuleLoader {
    /// <summary> Create rule Loader for making changes to project files </summary>
    public RuleLoader() : this(null, null) { }

    /// <summary> Create rule Loader for making changes to project files </summary>
    [ActivatorUtilitiesConstructor]
    public RuleLoader(IRuleSerializer serializer, IMessageLogger logger) : base(logger) {
        Serializer = serializer;
    }


    #region properties

    /// <summary> The rule serialize to use while loading </summary>
    public IRuleSerializer Serializer { get; set; }

    #endregion

    /// <inheritdoc />
    public Task<LoadRulesResponse> LoadRulesInProjectPath(string projectBasePath, string dbContextRulesFile = null) {
        return LoadRulesInProjectPath(new LoadOptions(projectBasePath: projectBasePath, dbContextRulesFile: dbContextRulesFile));
    }

    /// <inheritdoc />
    public async Task<LoadRulesResponse> LoadRulesInProjectPath(ILoadOptions request) {
        var response = new LoadRulesResponse(Logger);
        response.Log += OnResponseLog;
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

                var mask = request.DbContextRulesFile.ApplyContextNameMask("*");

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
                    response.GetInternals().LogError($"Error processing {fileInfo.Name}: {ex.Message}");
                }

            return response;
        } catch (Exception ex) {
            response.GetInternals().LogError($"Error: {ex.Message}");
            return response;
        } finally {
            response.Log -= OnResponseLog;
        }
    }

    private static async Task<T> TryReadRules<T>(FileInfo jsonFile, LoggedResponse loggedResponse, IRuleSerializer ruleSerializer)
        where T : class, new() {
        try {
            var rules = await ruleSerializer.Deserialize<T>(jsonFile.FullName);
            return rules;
        } catch (Exception ex) {
            loggedResponse?.GetInternals().LogError($"Unable to open {jsonFile.Name}: {ex.Message}");
            return null;
        }
    }
}

/// <summary> Response for load rules operation </summary>
public sealed class LoadRulesResponse : LoggedResponse {
    /// <inheritdoc />
    public LoadRulesResponse(IMessageLogger logger) : base(logger) { }

    /// <summary> The loaded rules </summary>
    public List<IRuleModelRoot> Rules { get; } = new();

    /// <inheritdoc />
    public override bool Success => base.Success && Rules.Count > 0;
}