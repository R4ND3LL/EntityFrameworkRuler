using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdmxRuler.Applicator;
using EdmxRuler.Common;
using EdmxRuler.Extensions;
using EdmxRuler.Generator;
using EdmxRuler.RuleModels.NavigationNaming;
using EdmxRuler.RuleModels.PrimitiveNaming;
using EdmxRuler.RuleModels.PropertyTypeChanging;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace EdmxRuler.Tests.General;

public sealed class GeneralTests {
    private readonly ITestOutputHelper output;

    public GeneralTests(ITestOutputHelper output) {
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
        generateRulesResponse.Rules.Count.ShouldBe(3);
        output.WriteLine($"Successfully generated {generateRulesResponse.Rules.Count} rule files in {elapsed}ms");
        rules.ShouldBe(generateRulesResponse.Rules);
        var enumMappingRules = rules.OfType<PropertyTypeChangingRules>().Single();
        var primitiveNamingRules = rules.OfType<PrimitiveNamingRules>().Single();
        var navigationNamingRules = rules.OfType<NavigationNamingRules>().Single();

        enumMappingRules.Classes.ForEach(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        enumMappingRules.Classes.SelectMany(o => o.Properties).ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        enumMappingRules.Classes.Count.ShouldBe(2);
        enumMappingRules.Classes[0].Name.ShouldBe("Order");
        enumMappingRules.Classes[1].Name.ShouldBe("Product");
        enumMappingRules.Classes[0].Properties.Count.ShouldBe(1);
        enumMappingRules.Classes[1].Properties.Count.ShouldBe(1);
        enumMappingRules.Classes[0].Properties[0].Name.ShouldBe("Freight");
        enumMappingRules.Classes[0].Properties[0].NewType
            .ShouldBe("NorthwindTestProject.Models.FreightEnum"); // internal type
        enumMappingRules.Classes[1].Properties[0].Name.ShouldBe("ReorderLevel");
        enumMappingRules.Classes[1].Properties[0].NewType.ShouldBe("Common.OrderLevelEnum"); // external type

        primitiveNamingRules.Schemas.Count.ShouldBe(1);
        primitiveNamingRules.Schemas[0].Tables.Count.ShouldBe(10);
        primitiveNamingRules.Schemas[0].Tables[0].Columns.Count.ShouldBe(0);
        primitiveNamingRules.Schemas[0].Tables[3].Columns.Count.ShouldBe(1);
        primitiveNamingRules.Schemas[0].Tables[4].Columns.Count.ShouldBe(1);
        primitiveNamingRules.Schemas[0].Tables[3].Columns[0].Name.ShouldBe("ReportsTo");
        primitiveNamingRules.Schemas[0].Tables[3].Columns[0].NewName.ShouldBe("ReportsToFk");
        primitiveNamingRules.Schemas[0].Tables.ForEach(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        primitiveNamingRules.Schemas[0].Tables.ForEach(o => o.NewName.IsValidSymbolName().ShouldBeTrue());
        primitiveNamingRules.Schemas[0].Tables.SelectMany(o => o.Columns)
            .ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        primitiveNamingRules.Schemas[0].Tables.SelectMany(o => o.Columns)
            .ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());

        navigationNamingRules.Namespace.ShouldBe("");
        navigationNamingRules.Classes.Count.ShouldBe(10);
        navigationNamingRules.Classes.All(o => o.Properties.Count > 0).ShouldBeTrue();
        navigationNamingRules.Classes[0].Properties[0].Name.Contains("ProductsCategoryIDNavigations").ShouldBeTrue();
        navigationNamingRules.Classes[0].Properties[0].Name.Contains("Products").ShouldBeTrue();
        navigationNamingRules.Classes[0].Properties[0].NewName.ShouldBe("Products");
        navigationNamingRules.Classes.ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        navigationNamingRules.Classes.SelectMany(o => o.Properties)
            .ForAll(o => o.Name.ForAll(n => n.IsValidSymbolName().ShouldBeTrue()));
        navigationNamingRules.Classes.SelectMany(o => o.Properties)
            .ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());

        output.WriteLine($"Rule contents look good");

        var csProj = ResolveNorthwindProject();
        var projBasePath = new FileInfo(csProj).Directory!.FullName;
        var applicator = new RuleApplicator(projBasePath);
        applicator.OnLog += LogReceived;
        start = DateTimeExtensions.GetTime();
        ApplyRulesResponse response;
        response = await applicator.ApplyRules(primitiveNamingRules);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count() == 1) response.Errors.First().ShouldStartWith("Error loading existing project");

        response.Information.Last()
            .ShouldStartWith("4 classes renamed, 2 properties renamed across ", Case.Insensitive);
        output.WriteLine($"Primitive naming rules applied correctly");

        response = await applicator.ApplyRules(navigationNamingRules);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count() == 1) response.Errors.First().ShouldStartWith("Error loading existing project");

        var renamed = response.Information.Where(o => o.StartsWith("Renamed")).ToArray();
        renamed.Length.ShouldBe(15);
        var couldNotFind = response.Information.Where(o => o.StartsWith("Could not find ")).ToArray();
        couldNotFind.Length.ShouldBe(1);
        response.Information.Last().ShouldStartWith("15 properties renamed across ", Case.Insensitive);

        output.WriteLine($"Navigation naming rules applied correctly");

        response = await applicator.ApplyRules(enumMappingRules);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count() == 1) response.Errors.First().ShouldStartWith("Error loading existing project");

        response.Information.Count(o => o.StartsWith("Update")).ShouldBe(2);
        response.Information.Last().ShouldStartWith("2 property types changed across 2 files", Case.Insensitive);
        output.WriteLine($"Enum mapping rules applied correctly");
        logReceivedCount.ShouldBeGreaterThan(30);
        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine(
            $"Completed in {elapsed}ms.  FindClassesByNameTime: {RoslynExtensions.FindClassesByNameTime}ms.  RenameClassAsyncTime: {RoslynExtensions.RenameClassAsyncTime}ms.  RenamePropertyAsyncTime: {RoslynExtensions.RenamePropertyAsyncTime}ms.  ChangePropertyTypeAsyncTime: {RoslynExtensions.ChangePropertyTypeAsyncTime}ms");

        void LogReceived(object sender, LogMessage logMessage) {
            logReceivedCount++;
        }
    }

    [Fact]
    public async Task TypeFinder() {
        var state = new RuleApplicator.RoslynProjectState();
        var response = new ApplyRulesResponse(null);
        await state.TryLoadProjectOrFallbackOnce(ResolveNorthwindProject(), null, null, response);
        var project = state.Project;
        project.ShouldNotBeNull();
        //var ns = "NorthwindTestProject.Models";
        var result = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindDeclarationsAsync(project, "Products",
            false,
            SymbolFilter.Type, CancellationToken.None);
        var results = result.Where(o => o.Kind == SymbolKind.NamedType)
            .OfType<ITypeSymbol>().Where(o => o.TypeKind == TypeKind.Class && !o.IsAnonymousType && !o.IsValueType)
            .ToList();
        results.Count.ShouldBe(1);
        var r = results[0];
        var syntaxReferences = r.DeclaringSyntaxReferences;
        foreach (var syntaxReference in syntaxReferences) {
            var root = await syntaxReference.SyntaxTree.GetRootAsync();
        }

        var compilation = await project.GetCompilationAsync();

        var type = compilation.GetTypeByMetadataName("NorthwindTestProject.Models.Products");
        var syntaxReferences2 = type?.DeclaringSyntaxReferences;
        syntaxReferences2.ShouldNotBeNull();
        foreach (var syntaxReference in syntaxReferences2) {
            var syntaxNode = syntaxReference.GetSyntax();
            var d = syntaxNode.DescendantNodesAndSelf().ToArray();
            var root = await syntaxReference.SyntaxTree.GetRootAsync();
        }
    }

    private static string ResolveNorthwindEdmxPath() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "src") dir = dir.Parent;

        dir.ShouldNotBeNull();
        var path = Path.Combine(dir.FullName, "Resources\\Northwind.edmx");
        File.Exists(path).ShouldBeTrue();
        return path;
    }

    private static string ResolveNorthwindProject() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "Tests") dir = dir.Parent;

        dir.ShouldNotBeNull();
        var path = Path.Combine(dir.FullName, "NorthwindTestProject\\NorthwindTestProject.csproj");
        File.Exists(path).ShouldBeTrue();
        return path;
    }

    private void Log(string msg) {
        output.WriteLine(msg);
        Debug.WriteLine(msg);
    }
}