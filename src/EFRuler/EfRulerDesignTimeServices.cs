using System.Diagnostics.CodeAnalysis;
using EdmxRuler;
using EntityFrameworkRuler.Services;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkRuler {
    /// <summary>
    /// Used to the configure design-time services for this library.
    /// </summary>
    public sealed class EfRulerDesignTimeServices : IDesignTimeServices {
        /// <summary>
        /// Adds this library's design-time services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
        public void ConfigureDesignTimeServices(IServiceCollection services) {
            services.AddSingleton<IPluralizer, EfPluralizer>();
            services.AddSingleton<IScaffoldingTypeMapper, EfScaffoldingTypeMapper>();
            services.AddSingleton<ICandidateNamingService, EfCandidateNamingService>();
            //services.AddSingleton<ICandidateNamingService, CandidateNamingService>();
            services.AddSingleton<EdmxRuler.Common.IRuleProvider, EdmxRuler.Common.DefaultRuleProvider>();
            services.AddRuleApplicator();
        }
    }
}