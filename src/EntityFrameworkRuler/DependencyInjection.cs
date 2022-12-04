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

public static class DependencyInjection {
    /// <summary> Add Entity Framework Ruler services </summary>
    public static T AddRuler<T>(this T serviceCollection) where T : IServiceCollection =>
        (T)serviceCollection
            .AddRulerCommon()
            .AddSingleton<IMessageLogger, ConsoleMessageLogger>()
            .AddTransient<IRuleApplicator, RuleApplicator>();


}