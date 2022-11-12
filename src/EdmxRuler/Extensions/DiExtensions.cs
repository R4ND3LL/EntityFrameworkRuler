using System;
using Microsoft.Extensions.DependencyInjection;

namespace WealthTrader.Application.Common.Extensions; 

public static class DiExtensions {
    /// <summary> return non-registered concrete class instance based on exact type given </summary>
    public static T GetAsSelf<T>(this IServiceProvider serviceProvider) => ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);

    /// <summary> return non-registered concrete class instance based on exact type given </summary>
    public static T GetConcrete<T>(this IServiceProvider serviceProvider) => ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);

    /// <summary> return non-registered concrete class instance based on exact type given </summary>
    public static object GetConcrete(this IServiceProvider serviceProvider, Type t) => ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, t);
}