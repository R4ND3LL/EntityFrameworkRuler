using EntityFrameworkRuler.Loader;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler;

public static class EntityFrameworkRulerExtensions {

    /// <summary> Add RuleLoader services </summary>
    public static T AddRuleLoader<T>(this T serviceCollection, LoaderOptions loaderOptions = null)
        where T : IServiceCollection =>
        (T)serviceCollection
            .AddSingleton(loaderOptions ?? new LoaderOptions() { ProjectBasePath = Directory.GetCurrentDirectory() })
            .AddSingleton<IRuleLoader, RuleLoader>()
            .CoerceLoaderServiceCollection();

  private static T CoerceLoaderServiceCollection<T>(this T serviceCollection) where T : IServiceCollection {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }
}