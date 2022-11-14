using System.Reflection;
using EntityFrameworkRuler.Rules.NavigationNaming;
using EntityFrameworkRuler.Rules.PrimitiveNaming;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Design.Services;

/// <summary> Loader of rule configuration for runtime ef scaffolding operations </summary>
public interface IRuleLoader {
    /// <summary> Load the respective rule info </summary>
    PrimitiveNamingRules GetPrimitiveNamingRules();

    // /// <summary> Load the respective rule info </summary>
    // PropertyTypeChangingRules GetPropertyTypeChangingRules();

    /// <summary> Load the respective rule info </summary>
    NavigationNamingRules GetNavigationNamingRules();

    /// <summary> Gets the ModelCodeGenerationOptions that describes the ef scaffolding context, such that rule info can be processed correctly. </summary>
    ModelCodeGenerationOptions CodeGenOptions { get; }

    /// <summary> Gets the ModelReverseEngineerOptions. </summary>
    ModelReverseEngineerOptions ReverseEngineerOptions { get; }

    /// <summary> Gets the solution folder assuming it was resolvable as a parent of the project folder. </summary>
    string SolutionPath { get; }

    /// <summary> Sets the ModelCodeGenerationOptions that describes the ef scaffolding context, such that rule info can be processed correctly. </summary>
    IRuleLoader SetCodeGenerationOptions(ModelCodeGenerationOptions options);

    /// <summary> Sets the ModelReverseEngineerOptions for the current scaffolding process.  This includes options for UseDatabaseNames, NoPluralize, etc. </summary>
    IRuleLoader SetReverseEngineerOptions(ModelReverseEngineerOptions options);

    /// <summary> The target project assembly, and relevant references.  Used to resolve custom property types from. </summary>
    IList<Assembly> TargetAssemblies { get; }
}