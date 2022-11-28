using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Saver;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class EntityFrameworkRulerExtensions {
    /// <summary> Add RuleLoader services from EntityFrameworkRuler.Common only </summary>
    public static T AddRulerCommon<T>(this T serviceCollection)
        where T : IServiceCollection =>
        (T)serviceCollection
            .AddSingleton<IRuleSerializer, JsonRuleSerializer>()
            .AddTransient<IRuleSaver, RuleSaver>()
            .AddTransient<IRuleLoader, RuleLoader>()
            .CoerceLoaderServiceCollection();

    private static T CoerceLoaderServiceCollection<T>(this T serviceCollection) where T : IServiceCollection {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }
}