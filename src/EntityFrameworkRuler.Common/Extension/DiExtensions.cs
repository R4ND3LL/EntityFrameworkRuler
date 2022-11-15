using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler.Extension;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class DiExtensions {
    /// <summary> return non-registered concrete class instance based on exact type given </summary>
    public static T GetAsSelf<T>(this IServiceProvider serviceProvider) =>
        ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);

    /// <summary> return non-registered concrete class instance based on exact type given </summary>
    public static T GetConcrete<T>(this IServiceProvider serviceProvider) =>
        ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);

    /// <summary> return non-registered concrete class instance based on exact type given </summary>
    public static object GetConcrete(this IServiceProvider serviceProvider, Type t) =>
        ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, t);
}