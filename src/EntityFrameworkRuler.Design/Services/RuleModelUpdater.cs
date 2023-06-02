using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Saver;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable ClassCanBeSealed.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

#pragma warning disable CS1591

namespace EntityFrameworkRuler.Design.Services;

/// <inheritdoc />
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuleModelUpdater : IRuleModelUpdater {
    private readonly IDesignTimeRuleLoader ruleLoader;
    private readonly IRuleSaver ruleSaver;
    private readonly IMessageLogger logger;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleModelUpdater(IDesignTimeRuleLoader ruleLoader, IRuleSaver ruleSaver, IMessageLogger logger) {
        this.ruleLoader = ruleLoader;
        this.ruleSaver = ruleSaver;
        this.logger = logger ?? NullMessageLogger.Instance;
    }

    /// <inheritdoc />
    public void OnModelCreated(IModel model) {
        if (ruleLoader == null) return;
        var contextName = ruleLoader.CodeGenOptions?.ContextName;
        var contextRules = ruleLoader.GetDbContextRules();
        if (contextRules is null) {
            logger.WriteVerbose($"New DB Context Rules file being initialized for {contextName ?? "No Name"}");
            contextRules = new(new() {
                IncludeUnknownSchemas = true
            });
        } else if (contextRules.Rule.Name.HasNonWhiteSpace() && contextName.IsNullOrWhiteSpace() && ruleLoader.CodeGenOptions != null)
            // we can actually set the context name because it is missing from the options, and defined in the rules
            if (contextRules.Rule.Name.IsValidSymbolName())
                ruleLoader.CodeGenOptions.ContextName = contextRules.Rule.Name;

        var start = DateTimeExtensions.GetTime();
        var projectDir = ruleLoader.GetProjectDir();
        if (projectDir.IsNullOrWhiteSpace()) {
            projectDir = contextRules.Rule.FilePath.HasNonWhiteSpace() ? Path.GetDirectoryName(contextRules.Rule.FilePath) : null;
            if (projectDir.IsNullOrWhiteSpace()) return;
        }

        if (contextName.HasNonWhiteSpace()) contextRules.Rule.Name = contextName;

        // initialize a rule file from the current reverse engineered model
        var saver = ruleSaver ?? new RuleSaver();
        var response = saver.SaveRules(projectDir, null, contextRules.Rule).GetAwaiter().GetResult();
        var elapsed = DateTimeExtensions.GetTime() - start;

        if (response.Errors.Any())
            logger.WriteError($"Failed to save rule file: {response.Errors.First()}");
        else if (response.SavedRules.Count > 0) {
            var fn = Path.GetFileName(response.SavedRules[0]);
            var action = contextRules.Rule.FilePath.IsNullOrWhiteSpace() ? "Created" : "Updated";
            logger.WriteInformation($"{action} {fn} in {elapsed}ms");
        }
    }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public interface IRuleModelUpdater {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void OnModelCreated(IModel model);
}