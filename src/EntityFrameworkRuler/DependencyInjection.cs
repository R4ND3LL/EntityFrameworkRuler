using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Common;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler;

public static class DependencyInjection {
    /// <summary> Add Entity Framework Ruler services </summary>
    public static T AddRulerCli<T>(this T serviceCollection) where T : IServiceCollection =>
        (T)serviceCollection?
            .AddRuler()
            .AddSingleton<IMessageLogger, ConsoleMessageLogger>()
            .AddTransient<IRuleApplicator, RuleApplicator>();


}