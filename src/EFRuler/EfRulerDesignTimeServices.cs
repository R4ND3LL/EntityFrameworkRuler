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
        public void ConfigureDesignTimeServices(IServiceCollection services) {
            services.AddSingleton<IPluralizer, EfPluralizer>();
            services.AddSingleton<ICandidateNamingService, EfCandidateNamingService>();
        }
    }
}