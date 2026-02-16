using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Design.Scaffolding.CodeGeneration;
using EntityFrameworkRuler.Design.Services;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler.Design {
    /// <summary>
    /// Used to the configure design-time services for this library.
    /// </summary>
    public sealed class RuledDesignTimeServices : IDesignTimeServices {
        /// <summary> Creates RuledDesignTimeServices </summary>
        public RuledDesignTimeServices() { }

        /// <summary> Adds this library's design-time services to the service collection. </summary>
        /// <param name="services">The service collection.</param>
        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
        public void ConfigureDesignTimeServices(IServiceCollection services) {
            services.AddSingleton<IPluralizer, RuledPluralizer>()
                //.AddSingleton<IScaffoldingTypeMapper, EfRulerScaffoldingTypeMapper>()
                .AddSingleton<ICandidateNamingService, RuledCandidateNamingService>()
                .AddSingleton<IScaffoldingModelFactory, RuledRelationalScaffoldingModelFactory>()
                .AddSingleton<IReverseEngineerScaffolder, RuledReverseEngineerScaffolder>()
                //.AddSingleton<ICandidateNamingService, CandidateNamingService>()
                .AddSingleton<IDesignTimeRuleLoader, DesignTimeRuleLoader>()
                .AddSingleton<IRuleModelUpdater, RuleModelUpdater>()
                .AddSingleton<IAnnotationCodeGenerator, RuledAnnotationCodeGenerator>()
                .AddSingleton<IExtraCodeGenerator, ExtraCodeGenerator>()
                .TryAddSingletonEnumerable<IRuledModelCodeGenerator, FunctionsModelGenerator>()
                .TryAddSingletonEnumerable<IRuledModelCodeGenerator, FunctionsInterfaceModelGenerator>()
                .TryAddSingletonEnumerable<IRuledModelCodeGenerator, DbContextFunctionsModelGenerator>()
                .TryAddSingletonEnumerable<IRuledModelCodeGenerator, DbContextExtensionsModelGenerator>()
                .TryAddSingletonEnumerable<IRuledModelCodeGenerator, UserDefinedTemplateModelGenerator>()
                //.TryAddSingletonEnumerable<IModelCodeGenerator, RuledTemplatedModelGenerator>()
                .AddRuler()
                .AddSingleton<IMessageLogger, EfConsoleMessageLogger>();
        }
    }
}