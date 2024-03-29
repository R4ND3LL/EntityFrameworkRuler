﻿using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Generator.EdmxModel;
using EntityFrameworkRuler.Generator.Services;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Saver;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class DependencyInjection {
    /// <summary> Add RuleLoader services from EntityFrameworkRuler.Common only </summary>
    public static IServiceCollection AddRuler(this IServiceCollection serviceCollection)
        => serviceCollection?
            .AddSingleton<IRuleSerializer, JsonRuleSerializer>()
            .AddTransient<IRulerNamingService, RulerNamingService>()
            .AddSingleton<IRulerPluralizer, HumanizerPluralizer>()
            .AddSingleton<IMessageLogger>(NullMessageLogger.Instance)
            .AddTransient<IEdmxParser, EdmxParser>()
            .AddTransient<IRuleSaver, RuleSaver>()
            .AddTransient<IRuleLoader, RuleLoader>()
            .AddTransient<IRuleGenerator, RuleGenerator>()
            .AddTransient<IRuleApplicator, RuleApplicator>();

    private static IServiceCollection CoerceLoaderServiceCollection(this IServiceCollection serviceCollection) {
        // possible location of reflection based service wiring on the target project ?
        return serviceCollection;
    }
}