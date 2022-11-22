using EntityFrameworkRuler.Loader;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class EntityFrameworkRulerExtensions {
    /// <summary> Add RuleLoader services </summary>
    public static T AddRuleLoader<T>(this T serviceCollection, LoadOptions loadOptions = null)
        where T : IServiceCollection =>
        (T)serviceCollection
            .AddSingleton(loadOptions ?? new LoadOptions() { ProjectBasePath = Directory.GetCurrentDirectory() })
            .AddSingleton<IRuleLoader, RuleLoader>()
            .CoerceLoaderServiceCollection();

    private static T CoerceLoaderServiceCollection<T>(this T serviceCollection) where T : IServiceCollection {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }
}