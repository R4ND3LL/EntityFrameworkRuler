using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Extension;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Rules;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkRuler.Tests;

public sealed class EdmxRulerTests {
    private readonly ITestOutputHelper output;

    public EdmxRulerTests(ITestOutputHelper output) {
        this.output = output;
    }

    [Fact]
    public async Task TestGenAndApply() {
        var edmxPath = ResolveNorthwindEdmxPath();
        var logReceivedCount = 0;
        var start = DateTimeExtensions.GetTime();
        var generator = new RuleGenerator(edmxPath);
        generator.OnLog += LogReceived;
        var generateRulesResponse = generator.TryGenerateRules();
        var rules = generateRulesResponse.Rules;
        var elapsed = DateTimeExtensions.GetTime() - start;
        generateRulesResponse.Errors.Count().ShouldBe(0);
        generateRulesResponse.Rules.Count.ShouldBe(1);
        output.WriteLine($"Successfully generated {generateRulesResponse.Rules.Count} rule files in {elapsed}ms");
        rules.ShouldBe(generateRulesResponse.Rules);
        var enumMappingRules = rules.OfType<DbContextRule>().Single().Schemas.SelectMany(o => o.Tables).SelectMany(o => o.Columns)
            .Where(o => o.NewType.HasNonWhiteSpace()).ToList();
        var dbContextRule = rules.OfType<DbContextRule>().Single();
        var navigationNamingRules = rules.OfType<DbContextRule>().Single().Schemas
            .SelectMany(o => o.Tables)
            .SelectMany(o => o.Navigations)
            .ToList();

        enumMappingRules.Count.ShouldBe(2);
        enumMappingRules.ForEach(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        enumMappingRules.ForAll(o => (o.PropertyName == null || o.PropertyName.IsValidSymbolName()).ShouldBeTrue());

        dbContextRule.Schemas.Count.ShouldBe(1);
        dbContextRule.Schemas[0].Tables.Count.ShouldBe(27);
        var prod = dbContextRule.Schemas[0].Tables.FirstOrDefault(o => o.Name == "Products");
        prod.ShouldNotBeNull();
        prod.Columns.Count.ShouldBe(10);
        prod.Columns[0].PropertyName.ShouldBe("ProductId");
        prod.Columns[0].NewName.ShouldBe("ProductID");
        dbContextRule.Schemas[0].Tables.ForAll(o => (o.EntityName?.IsValidSymbolName() != false).ShouldBeTrue());
        dbContextRule.Schemas[0].Tables.ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());
        dbContextRule.Schemas[0].Tables.SelectMany(o => o.Columns)
            .ForAll(o => (o.PropertyName?.IsValidSymbolName() != false).ShouldBeTrue());
        dbContextRule.Schemas[0].Tables.SelectMany(o => o.Columns)
            .ForAll(o => (o.NewName == null || o.NewName.IsValidSymbolName()).ShouldBeTrue());

        navigationNamingRules.Count.ShouldBeGreaterThan(15);
        var fkOrdersCustomersRules = navigationNamingRules.Where(o => o.FkName == "FK_Orders_Customers").ToArray();
        fkOrdersCustomersRules.Length.ShouldBe(2);
        fkOrdersCustomersRules.Any(o => o.Name.Contains("Orders")).ShouldBeTrue();
        fkOrdersCustomersRules.Any(o => o.Name.Contains("CustomerNavigation")).ShouldBeTrue();
        fkOrdersCustomersRules.Any(o => o.NewName == "OrdersCustom").ShouldBeTrue();

        navigationNamingRules.ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());
        navigationNamingRules.ForAll(o => o.Name.ForAll(n => n.IsValidSymbolName().ShouldBeTrue()));

        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Rule contents look good at {elapsed}ms");

        var csProj = ResolveNorthwindProject();
        var projBasePath = new FileInfo(csProj).Directory!.FullName;
        var applicator = new RuleApplicator(projBasePath, adhocOnly: true);
        applicator.OnLog += LogReceived;
        start = DateTimeExtensions.GetTime();
        ApplyRulesResponse response;
        response = await applicator.ApplyRules(dbContextRule);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count() == 1) response.Errors.First().ShouldStartWith("Error loading existing project");

        response.Information.Last()
            .ShouldStartWith("16 classes renamed, 60 properties renamed, 2 property types changed across 2", Case.Insensitive);
        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"DbContext rules applied correctly at {elapsed}ms");

        var renamed = response.Information.Where(o => o.StartsWith("Renamed")).ToArray();
        renamed.Length.ShouldBeGreaterThan(70);
        var couldNotFind = response.Information.Where(o => o.StartsWith("Could not find ") && !o.Contains("Sysdiagram")).ToArray();
        couldNotFind.Length.ShouldBe(0);
#if DEBUG
        output.WriteLine(
            $"FindClassesByNameTime: {RoslynExtensions.FindClassesByNameTime}ms.  RenameClassAsyncTime: {RoslynExtensions.RenameClassAsyncTime}ms.  RenamePropertyAsyncTime: {RoslynExtensions.RenamePropertyAsyncTime}ms.  ChangePropertyTypeAsyncTime: {RoslynExtensions.ChangePropertyTypeAsyncTime}ms");
#endif

        void LogReceived(object sender, LogMessage logMessage) {
            logReceivedCount++;
        }
    }

    [Fact]
    public async Task ShouldLoadProjectUsingRoslynAndFindTypes() {
        var projectBasePath = ResolveNorthwindProject();
        var state = new RuleApplicator.RoslynProjectState(new RuleApplicator(projectBasePath));
        var response = new ApplyRulesResponse(null);
        await state.TryLoadProjectOrFallbackOnce(projectBasePath, null, null, response);
        var project = state.Project;
        project.ShouldNotBeNull();
        //var ns = "NorthwindTestProject.Models";
        var result = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindDeclarationsAsync(project, "Product",
            false,
            SymbolFilter.Type, CancellationToken.None);
        var results = result.Where(o => o.Kind == SymbolKind.NamedType)
            .OfType<ITypeSymbol>().Where(o => o.TypeKind == TypeKind.Class && !o.IsAnonymousType && !o.IsValueType)
            .ToList();
        results.Count.ShouldBe(1);

        var compilation = await project.GetCompilationAsync();
        compilation.ShouldNotBeNull();
        var type = compilation.GetTypeByMetadataName("NorthwindTestProject.Models.Product");
        var syntaxReferences2 = type?.DeclaringSyntaxReferences;
        syntaxReferences2.ShouldNotBeNull();
    }

    [Fact]
    public async Task ShouldLoadRules() {
        var start = DateTimeExtensions.GetTime();
        var ruleApplicator = new RuleApplicator(ResolveNorthwindProject());
        var rules = await ruleApplicator.LoadRulesInProjectPath();
        rules.ShouldNotBeNull();
        rules.Rules.ShouldNotBeNull();
        rules.Rules.Count.ShouldBeGreaterThan(0);
        var elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Loaded {rules.Rules.Count} in {elapsed}ms.");
    }

    // [Fact]
    // public void TestPurlalizer() {
    //     var words = new List<string>() {
    //         "Employee"
    //     };
    //     var bp = new Bricelam.EntityFrameworkCore.Design.Pluralizer();
    //     foreach (var word in words) {
    //         var bw = bp.Pluralize(word);
    //     }
    // }

    private static string ResolveNorthwindEdmxPath() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "Tests") dir = dir.Parent;

        dir.ShouldNotBeNull();
        var path = Path.Combine(dir.FullName, $"NorthwindTestEdmx{Path.DirectorySeparatorChar}Northwind.edmx");
        File.Exists(path).ShouldBeTrue();
        return path;
    }

    private static string ResolveNorthwindProject() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "Tests") dir = dir.Parent;

        dir.ShouldNotBeNull();
        var path = Path.Combine(dir.FullName, $"NorthwindTestProject{Path.DirectorySeparatorChar}NorthwindTestProject.csproj");
        File.Exists(path).ShouldBeTrue();
        return path;
    }

    private void Log(string msg) {
        output.WriteLine(msg);
        Debug.WriteLine(msg);
    }
}