using System.IO;
using EdmxRuler.Applicator;
using EdmxRuler.Generator;
using EdmxRuler.Generator.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBeInternal

namespace EdmxRuler;

public static class EdmxRulerExtensions {
    /// <summary> Add RuleGenerator services </summary>
    public static T AddRuleGenerator<T>(this T serviceCollection, GeneratorOptions generatorOptions = null) where T : IServiceCollection =>
        (T)serviceCollection
            .AddSingleton(generatorOptions ?? GeneratorArgHelper.GetDefaultOptions())
            .AddSingleton<IEdmxRulerNamingService, EdmxRulerNamingService>()
            .AddSingleton<IEdmxRulerPluralizer, HumanizerPluralizer>()
            .AddSingleton<IRuleGenerator, RuleGenerator>()
            .CoerceServiceCollection();

    /// <summary> Add RuleApplicator services </summary>
    public static T AddRuleApplicator<T>(this T serviceCollection, ApplicatorOptions applicatorOptions = null)
        where T : IServiceCollection =>
        (T)serviceCollection
            .AddSingleton(applicatorOptions ?? ApplicatorArgHelper.GetDefaultOptions())
            .AddSingleton<IRuleApplicator, RuleApplicator>()
            .CoerceServiceCollection();

    private static T CoerceServiceCollection<T>(this T serviceCollection) where T : IServiceCollection {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }
}