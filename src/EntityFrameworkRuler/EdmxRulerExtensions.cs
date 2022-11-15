using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Generator.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler;

public static class EntityFrameworkRulerExtensions {
    /// <summary> Add RuleGenerator services </summary>
    public static T AddRuleGenerator<T>(this T serviceCollection, GeneratorOptions generatorOptions = null) where T : IServiceCollection =>
        (T)serviceCollection
            .AddSingleton(generatorOptions ?? GeneratorArgHelper.GetDefaultOptions())
            .AddSingleton<IRulerNamingService, RulerNamingService>()
            .AddSingleton<IRulerPluralizer, HumanizerPluralizer>()
            .AddSingleton<IRuleGenerator, RuleGenerator>()
            .CoerceGeneratorServiceCollection();

    /// <summary> Add RuleApplicator services </summary>
    public static T AddRuleApplicator<T>(this T serviceCollection, ApplicatorOptions applicatorOptions = null)
        where T : IServiceCollection =>
        (T)serviceCollection
            .AddSingleton(applicatorOptions ?? ApplicatorArgHelper.GetDefaultOptions())
            .AddSingleton<IRuleApplicator, RuleApplicator>()
            .CoerceApplicatorServiceCollection();

    private static T CoerceGeneratorServiceCollection<T>(this T serviceCollection) where T : IServiceCollection {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }

    private static T CoerceApplicatorServiceCollection<T>(this T serviceCollection) where T : IServiceCollection {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }
}