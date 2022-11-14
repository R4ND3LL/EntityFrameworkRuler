using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkRuler.Design.Services;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler.Design {
    /// <summary>
    /// Used to the configure design-time services for this library.
    /// </summary>
    public sealed class EfRulerDesignTimeServices : IDesignTimeServices {
        /// <summary> Creates EfRulerDesignTimeServices </summary>
        public EfRulerDesignTimeServices() {
#if DEBUG
            if (Debugger.IsAttached) return;
            var entryAssembly = Assembly.GetEntryAssembly();
            var entryName = entryAssembly?.GetName().Name;
            if (entryName.In("ef", "dotnet-ef")) Debugger.Launch();
#endif
        }

        /// <summary> Adds this library's design-time services to the service collection. </summary>
        /// <param name="services">The service collection.</param>
        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
        public void ConfigureDesignTimeServices(IServiceCollection services) {
            services.AddSingleton<IPluralizer, EfRulerPluralizer>();
            //services.AddSingleton<IScaffoldingTypeMapper, EfRulerScaffoldingTypeMapper>();
            services.AddSingleton<ICandidateNamingService, EfRulerCandidateNamingService>();
            services.AddSingleton<IScaffoldingModelFactory, EfRulerRelationalScaffoldingModelFactory>();
            services.AddSingleton<IReverseEngineerScaffolder, EfRulerReverseEngineerScaffolder>();
            //services.AddSingleton<ICandidateNamingService, CandidateNamingService>();
            services.AddSingleton<IRuleLoader, DefaultRuleLoader>();
            services.AddRuleApplicator();
        }
    }
}