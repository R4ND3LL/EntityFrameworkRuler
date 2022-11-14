using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Services;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EntityFrameworkRuler {
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
    public class EfRulerDesignTimeServicesTests {
        [Fact]
        public void ConfigureDesignTimeServices_works() {
            var serviceCollection = new ServiceCollection();

            new EfRulerDesignTimeServices().ConfigureDesignTimeServices(serviceCollection);
            serviceCollection.AddEntityFrameworkDesignTimeServices();

            serviceCollection.AddSingleton(new Moq.Mock<IRelationalTypeMappingSource>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<IModelRuntimeInitializer>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<IDatabaseModelFactory>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<ITypeMappingSource>().Object);
            // for net6:
            serviceCollection.AddSingleton(new Moq.Mock<Microsoft.EntityFrameworkCore.Diagnostics.LoggingDefinitions>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<IProviderConfigurationCodeGenerator>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<IAnnotationCodeGenerator>().Object);

            using var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            var p = serviceProvider.GetService<IPluralizer>();
            Assert.IsType<EfRulerPluralizer>(p);
            Assert.IsType<EfRulerCandidateNamingService>(serviceProvider.GetService<ICandidateNamingService>());
            Assert.IsType<EfRulerRelationalScaffoldingModelFactory>(serviceProvider.GetService<IScaffoldingModelFactory>());
            Assert.IsType<EfRulerReverseEngineerScaffolder>(serviceProvider.GetService<IReverseEngineerScaffolder>());
            Assert.IsType<DefaultRuleLoader>(serviceProvider.GetService<IRuleLoader>());
        }

        [Fact]
        public void ConfigureDesignTimeServices_works_with_override() {
            var serviceCollection = new ServiceCollection();

            new EfRulerDesignTimeServices().ConfigureDesignTimeServices(serviceCollection);
            serviceCollection.AddEntityFrameworkDesignTimeServices();

            serviceCollection.AddSingleton(new Moq.Mock<IRelationalTypeMappingSource>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<IModelRuntimeInitializer>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<IDatabaseModelFactory>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<ITypeMappingSource>().Object);
            // for net6:
            serviceCollection.AddSingleton(new Moq.Mock<Microsoft.EntityFrameworkCore.Diagnostics.LoggingDefinitions>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<IProviderConfigurationCodeGenerator>().Object);
            serviceCollection.AddSingleton(new Moq.Mock<IAnnotationCodeGenerator>().Object);

            // another custom override
            var scaffoldingModelFactory = new Moq.Mock<IScaffoldingModelFactory>().Object;
            serviceCollection.AddSingleton(scaffoldingModelFactory);
            var candidateNamingService = new Moq.Mock<ICandidateNamingService>().Object;
            serviceCollection.AddSingleton(candidateNamingService);

            using var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            var p = serviceProvider.GetService<IPluralizer>();
            Assert.IsType<EfRulerPluralizer>(p);
            Assert.IsType<EfRulerReverseEngineerScaffolder>(serviceProvider.GetService<IReverseEngineerScaffolder>());
            Assert.IsType<DefaultRuleLoader>(serviceProvider.GetService<IRuleLoader>());

            var actualIScaffoldingModelFactory = serviceProvider.GetService<IScaffoldingModelFactory>();
            actualIScaffoldingModelFactory.ShouldBe(scaffoldingModelFactory);

            var actualICandidateNamingService = serviceProvider.GetService<ICandidateNamingService>();
            actualICandidateNamingService.ShouldBe(candidateNamingService);
        }
    }
}