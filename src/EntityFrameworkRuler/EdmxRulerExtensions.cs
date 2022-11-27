using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Generator.EdmxModel;
using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Saver;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler;

public static class EntityFrameworkRulerExtensions {
    /// <summary> Add Entity Framework Ruler services </summary>
    public static T AddRuler<T>(this T serviceCollection) where T : IServiceCollection =>
        (T)serviceCollection
            .AddTransient<IRulerNamingService, RulerNamingService>()
            .AddSingleton<IRulerPluralizer, HumanizerPluralizer>()
            .AddTransient<IEdmxParser, EdmxParser>()
            .AddSingleton<IRuleSerializer, JsonRuleSerializer>()
            .AddTransient<IRuleSaver, RuleSaver>()
            .AddTransient<IRuleLoader, RuleLoader>()
            .AddTransient<IRuleGenerator, RuleGenerator>()
            .AddTransient<IRuleApplicator, RuleApplicator>()
            .CoerceServiceCollection();

    private static T CoerceServiceCollection<T>(this T serviceCollection) where T : IServiceCollection {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }
}