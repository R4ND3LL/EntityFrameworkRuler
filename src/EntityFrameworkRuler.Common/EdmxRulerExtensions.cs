using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Saver;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class EntityFrameworkRulerExtensions {
    /// <summary> Add RuleLoader services from EntityFrameworkRuler.Common only </summary>
    public static IServiceCollection AddRulerCommon(this IServiceCollection serviceCollection)
        =>
        serviceCollection
            .AddSingleton<IRuleSerializer, JsonRuleSerializer>()
            .AddTransient<IRuleSaver, RuleSaver>()
            .AddTransient<IRuleLoader, RuleLoader>()
            .CoerceLoaderServiceCollection();

    private static IServiceCollection CoerceLoaderServiceCollection(this IServiceCollection serviceCollection) {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }
}