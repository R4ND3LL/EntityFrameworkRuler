using EntityFrameworkRuler.Services;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkRuler {
    public class EfRulerDesignTimeServicesTests {
        [Fact]
        public void ConfigureDesignTimeServices_works() {
            var serviceCollection = new ServiceCollection();

            new EfRulerDesignTimeServices().ConfigureDesignTimeServices(serviceCollection);
            serviceCollection.AddEntityFrameworkDesignTimeServices();

            using var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            var p = serviceProvider.GetService<IPluralizer>();
            Assert.IsType<EfPluralizer>(p);
        }
    }
}