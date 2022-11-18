using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Services;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Castle.DynamicProxy;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Extension;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Tests;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuledDesignTimeServicesTests {
    [Fact]
    public void ConfigureDesignTimeServices_works() {
        var serviceCollection = GetDefaultServiceCollection();

        using var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        var p = serviceProvider.GetService<IPluralizer>();
        Assert.IsType<RuledPluralizer>(p);
        Assert.IsType<RuledCandidateNamingService>(serviceProvider.GetService<ICandidateNamingService>());
        Assert.IsType<RuledRelationalScaffoldingModelFactory>(serviceProvider.GetService<IScaffoldingModelFactory>());
        Assert.IsType<RuledReverseEngineerScaffolder>(serviceProvider.GetService<IReverseEngineerScaffolder>());
        Assert.IsType<DesignTimeRuleLoader>(serviceProvider.GetService<IDesignTimeRuleLoader>());
    }

    private static ServiceCollection GetDefaultServiceCollection() {
        var serviceCollection = new ServiceCollection();

        new RuledDesignTimeServices().ConfigureDesignTimeServices(serviceCollection);
        serviceCollection.AddEntityFrameworkDesignTimeServices();

        serviceCollection.AddSingleton(new Moq.Mock<IRelationalTypeMappingSource>().Object);
        serviceCollection.AddSingleton(new Moq.Mock<IModelRuntimeInitializer>().Object);
        serviceCollection.AddSingleton(new Moq.Mock<IDatabaseModelFactory>().Object);
        serviceCollection.AddSingleton(new Moq.Mock<ITypeMappingSource>().Object);
        // for net6:
        serviceCollection.AddSingleton(new Moq.Mock<Microsoft.EntityFrameworkCore.Diagnostics.LoggingDefinitions>().Object);
        serviceCollection.AddSingleton(new Moq.Mock<IProviderConfigurationCodeGenerator>().Object);
        serviceCollection.AddSingleton(new Moq.Mock<IAnnotationCodeGenerator>().Object);
        return serviceCollection;
    }

    [Fact]
    public void ConfigureDesignTimeServices_works_with_override() {
        var serviceCollection = GetDefaultServiceCollection();

        // another custom override
        var scaffoldingModelFactory = new Moq.Mock<IScaffoldingModelFactory>().Object;
        serviceCollection.AddSingleton(scaffoldingModelFactory);
        var candidateNamingService = new Moq.Mock<ICandidateNamingService>().Object;
        serviceCollection.AddSingleton(candidateNamingService);

        using var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        var p = serviceProvider.GetService<IPluralizer>();
        Assert.IsType<RuledPluralizer>(p);
        Assert.IsType<RuledReverseEngineerScaffolder>(serviceProvider.GetService<IReverseEngineerScaffolder>());
        Assert.IsType<DesignTimeRuleLoader>(serviceProvider.GetService<IDesignTimeRuleLoader>());

        var actualIScaffoldingModelFactory = serviceProvider.GetService<IScaffoldingModelFactory>();
        actualIScaffoldingModelFactory.ShouldBe(scaffoldingModelFactory);

        var actualICandidateNamingService = serviceProvider.GetService<ICandidateNamingService>();
        actualICandidateNamingService.ShouldBe(candidateNamingService);
    }

    [Fact]
    public void ScaffoldingModelFactoryProxyWorks() {
        var serviceCollection = GetDefaultServiceCollection();
        serviceCollection.AddSingleton<IScaffoldingModelFactory, ScaffoldingModelFactoryTestInterceptor>();
        using var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        var mf = serviceProvider.GetRequiredService<IScaffoldingModelFactory>();
        mf.ShouldBeOfType<ScaffoldingModelFactoryTestInterceptor>();
        var intercepted = (ScaffoldingModelFactoryTestInterceptor)mf;
        var proxyObject = intercepted.Initialize();
        proxyObject.ShouldNotBeNull();
        mf.Create(null, null);
        intercepted.InterceptedCallCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void T4ResourceLoading() {
        var assembly = typeof(DesignTimeRuleLoader).Assembly;
        var text = assembly.GetResourceText("EntityFrameworkRuler.Design.Resources.EntityTypeConfiguration.t4");
        text.IsNullOrWhiteSpace().ShouldBeFalse();
    }
}

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed class ScaffoldingModelFactoryTestInterceptor : IScaffoldingModelFactory, IInterceptor {
    private readonly IServiceProvider serviceProvider;
    private RelationalScaffoldingModelFactory proxy;

    public ScaffoldingModelFactoryTestInterceptor(IServiceProvider serviceProvider) {
        this.serviceProvider = serviceProvider;
    }

    internal RelationalScaffoldingModelFactory Initialize() {
        return proxy ??= serviceProvider.CreateClassProxy<RelationalScaffoldingModelFactory>(this);
    }

    public IModel Create(DatabaseModel databaseModel, ModelReverseEngineerOptions options) {
        proxy ??= Initialize();
        return proxy!.Create(databaseModel, options);
    }

    public readonly List<IInvocation> Invocations = new();
    public int InterceptedCallCount => Invocations.Count;

    void IInterceptor.Intercept(IInvocation invocation) {
        Invocations.Add(invocation);
    }
}