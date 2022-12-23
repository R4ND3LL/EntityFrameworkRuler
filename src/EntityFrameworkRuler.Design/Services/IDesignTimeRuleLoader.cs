using System.Reflection;
using EntityFrameworkRuler.Design.Services.Models;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Design.Services;

/// <summary> Loader of rule configuration for runtime ef scaffolding operations </summary>
public interface IDesignTimeRuleLoader {
    /// <summary> Load the respective rule info </summary>
    DbContextRuleNode GetDbContextRules();

    /// <summary> Gets the ModelCodeGenerationOptions that describes the ef scaffolding context, such that rule info can be processed correctly. </summary>
    ModelCodeGenerationOptions CodeGenOptions { get; }

    /// <summary> Gets the ModelReverseEngineerOptions. </summary>
    ModelReverseEngineerOptions ReverseEngineerOptions { get; }

    /// <summary> The detected entity framework version.  </summary>
    Version EfVersion { get; }

    /// <summary> Get the project base folder where the EF context model is being built </summary>
    string GetProjectDir();

    /// <summary> Gets the solution folder assuming it was resolvable as a parent of the project folder. </summary>
    string SolutionPath { get; }

    /// <summary> Sets the ModelCodeGenerationOptions that describes the ef scaffolding context, such that rule info can be processed correctly. </summary>
    IDesignTimeRuleLoader SetCodeGenerationOptions(ModelCodeGenerationOptions options);

    /// <summary> Sets the ModelReverseEngineerOptions for the current scaffolding process.  This includes options for UseDatabaseNames, NoPluralize, etc. </summary>
    IDesignTimeRuleLoader SetReverseEngineerOptions(ModelReverseEngineerOptions options);

    /// <summary> The target project assembly, and relevant references.  Used to resolve custom property types from. </summary>
    IEnumerable<Assembly> TargetAssemblies { get; }
}