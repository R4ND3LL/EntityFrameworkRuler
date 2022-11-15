using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using IInterceptor = Castle.DynamicProxy.IInterceptor;

namespace EntityFrameworkRuler.Design.Extensions;

/// <summary> Dynamic Proxy extensions </summary>
public static class ProxyExtensions {
    /// <summary>
    /// Create class proxy for the given object, filling in any constructor arguments automatically by
    /// pulling services from the service provider.
    /// </summary>
    public static T CreateClassProxy<T>(this IServiceProvider serviceProvider, params IInterceptor[] interceptors) where T : class {
        var type = typeof(T);
        var generator = new ProxyGenerator();
        var constructors = type.GetConstructors();
        var constructor = constructors.FirstOrDefault();
        var parameters = constructor?.GetParameters();
        if (parameters?.Length > 0) {
            // build param list using service provider
            var args = new List<object>();
            foreach (var parameter in parameters) {
                var service = serviceProvider.GetRequiredService(parameter.ParameterType);
                args.Add(service);
            }

            var proxy = generator.CreateClassProxy(type, args.ToArray(), interceptors);
            return (T)proxy;
        } else {
            var proxy = generator.CreateClassProxy<T>(interceptors);
            return proxy;
        }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static MethodInfo GetMethod<TArg1>(this Type t, string name,
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) {
        return t.GetMethod(name, flags, null, new[] { typeof(TArg1) }, null);
    }
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static MethodInfo GetMethod<TArg1, TArg2>(this Type t, string name,
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) {
        return t.GetMethod(name, flags, null, new[] { typeof(TArg1), typeof(TArg2) }, null);
    }
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static MethodInfo GetMethod<TArg1, TArg2, TArg3>(this Type t, string name,
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) {
        return t.GetMethod(name, flags, null, new[] { typeof(TArg1), typeof(TArg2), typeof(TArg3) }, null);
    }
}