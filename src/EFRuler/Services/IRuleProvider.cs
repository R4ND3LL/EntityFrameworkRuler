using System.Reflection;
using EdmxRuler.Rules.NavigationNaming;
using EdmxRuler.Rules.PrimitiveNaming;
using EdmxRuler.Rules.PropertyTypeChanging;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Services;

/// <summary> Loader of rule configuration for runtime ef scaffolding operations </summary>
public interface IRuleLoader {
    /// <summary> Load the respective rule info </summary>
    PrimitiveNamingRules GetPrimitiveNamingRules();

    /// <summary> Load the respective rule info </summary>
    PropertyTypeChangingRules GetPropertyTypeChangingRules();

    /// <summary> Load the respective rule info </summary>
    NavigationNamingRules GetNavigationNamingRules();

    /// <summary> Gets the ModelCodeGenerationOptions that describes the ef scaffolding context, such that rule info can be processed correctly. </summary>
    ModelCodeGenerationOptions CodeGenOptions { get; }

    /// <summary> Sets the ModelCodeGenerationOptions that describes the ef scaffolding context, such that rule info can be processed correctly. </summary>
    IRuleLoader SetCodeGenerationOptions(ModelCodeGenerationOptions options);

    IList<Assembly> TargetAssemblies { get; }
}