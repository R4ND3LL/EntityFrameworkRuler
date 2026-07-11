using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EntityFrameworkRuler.Common.Annotations;
using EntityFrameworkRuler.Design.Metadata.Builders;
using EntityFrameworkRuler.Design.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
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
using RulerTextTemplatingEngineHost = EntityFrameworkRuler.Design.Scaffolding.Internal.TextTemplatingEngineHost;

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

    [Fact]
    public void DbContextTemplate_omits_function_api_when_model_has_no_functions() {
        var generated = GenerateDbContextFunctionMembers(new ModelBuilder().Model, "NoFunctionsContext");

        generated.ShouldContain("public partial class NoFunctionsContext : DbContext");
        generated.ShouldNotContain("INoFunctionsContextFunctions");
        generated.ShouldNotContain("public virtual INoFunctionsContextFunctions Functions");
    }

    [Fact]
    public void DbContextTemplate_emits_function_api_when_model_has_functions() {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Model.SetAnnotation(RulerAnnotations.HasFunctions, true);

        var generated = GenerateDbContextFunctionMembers(modelBuilder.Model, "FunctionsContext");

        generated.ShouldContain("IFunctionsContextFunctions");
        generated.ShouldContain("public virtual IFunctionsContextFunctions Functions");
        generated.ShouldNotContain(RulerAnnotations.HasFunctions);
    }

    [Fact]
    public void ModelEx_CreateFunction_marks_model_as_having_functions() {
        var modelBuilder = new ModelBuilderEx(new ModelBuilder());

        modelBuilder.CreateFunction("ScalarFunction");

        modelBuilder.Model[RulerAnnotations.HasFunctions].ShouldBe(true);
    }

    [Fact]
    public void AnnotationCodeGenerator_ignores_has_functions_marker() {
        var model = new ModelBuilder().Model;
        model.SetAnnotation(RulerAnnotations.HasFunctions, true);
        using var serviceProvider = GetDefaultServiceCollection().BuildServiceProvider(validateScopes: true);
        var dependencies = new AnnotationCodeGeneratorDependencies(serviceProvider.GetRequiredService<IRelationalTypeMappingSource>());
        var generator = new RuledAnnotationCodeGenerator(dependencies);

        generator.FilterIgnoredAnnotations(model.GetAnnotations())
            .ShouldNotContain(annotation => annotation.Name == RulerAnnotations.HasFunctions);
    }

    private static string GenerateDbContextFunctionMembers(object model, string contextName) {
        var assembly = typeof(DesignTimeRuleLoader).Assembly;
        var template = assembly.GetResourceText("EntityFrameworkRuler.Design.Resources.DbContext.t4");
        var functionMembersEnd = template.IndexOf("            foreach (var entityType", StringComparison.Ordinal);
        Assert.True(functionMembersEnd > 0);
        template = template[..functionMembersEnd] + "#>\n}";
        var options = new ModelCodeGenerationOptions {
            ContextName = contextName,
            ContextNamespace = "SmokeTest",
            ModelNamespace = "SmokeTest",
            SuppressOnConfiguring = true
        };

        using var serviceProvider = GetDefaultServiceCollection().BuildServiceProvider(validateScopes: true);
        var host = new RulerTextTemplatingEngineHost(serviceProvider) { TemplateFile = "DbContext.t4" };
        host.Initialize();
        host.Session.Add("Model", model);
        host.Session.Add("Options", options);
        host.Session.Add("NamespaceHint", options.ContextNamespace);

        var generated = new TemplateTestGenerator().Generate(template, host);
        Assert.False(host.Errors.HasErrors, string.Join(Environment.NewLine, host.Errors.Cast<System.CodeDom.Compiler.CompilerError>().Select(error => error.ErrorText)));
        return generated;
    }
}

internal sealed class TemplateTestGenerator : RuledModelGeneratorBase {
    public TemplateTestGenerator() : base(new Moq.Mock<IOperationReporter>().Object) { }

    public string Generate(string template, RulerTextTemplatingEngineHost host) => Engine.ProcessTemplateAsync(template, host).GetAwaiter().GetResult();
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
