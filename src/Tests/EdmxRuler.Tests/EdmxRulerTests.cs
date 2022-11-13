using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdmxRuler.Applicator;
using EdmxRuler.Common;
using EdmxRuler.Extensions;
using EdmxRuler.Generator;
using EdmxRuler.Rules.NavigationNaming;
using EdmxRuler.Rules.PrimitiveNaming;
using EdmxRuler.Rules.PropertyTypeChanging;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace EdmxRuler.Tests;

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
        generateRulesResponse.Rules.Count.ShouldBe(3);
        output.WriteLine($"Successfully generated {generateRulesResponse.Rules.Count} rule files in {elapsed}ms");
        rules.ShouldBe(generateRulesResponse.Rules);
        var enumMappingRules = rules.OfType<PropertyTypeChangingRules>().Single();
        var primitiveNamingRules = rules.OfType<PrimitiveNamingRules>().Single();
        var navigationNamingRules = rules.OfType<NavigationNamingRules>().Single();

        enumMappingRules.Classes.ForEach(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        enumMappingRules.Classes.SelectMany(o => o.Properties).ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        enumMappingRules.Classes.Count.ShouldBe(2);
        enumMappingRules.Classes[0].Name.ShouldBe("Order_Detail");
        enumMappingRules.Classes[1].Name.ShouldBe("Products_by_Category");
        enumMappingRules.Classes[0].Properties.Count.ShouldBe(1);
        enumMappingRules.Classes[1].Properties.Count.ShouldBe(1);
        enumMappingRules.Classes[0].Properties[0].Name.ShouldBe("Quantity");
        enumMappingRules.Classes[0].Properties[0].NewType
            .ShouldBe("NorthwindModel.QuantityEnum"); // internal type
        enumMappingRules.Classes[1].Properties[0].Name.ShouldBe("UnitsInStockCustom");
        enumMappingRules.Classes[1].Properties[0].NewType.ShouldBe("NorthwindModel.UnitsInStockEnum"); // external type

        primitiveNamingRules.Schemas.Count.ShouldBe(1);
        primitiveNamingRules.Schemas[0].Tables.Count.ShouldBe(27);
        primitiveNamingRules.Schemas[0].Tables[0].Columns.Count.ShouldBe(4);
        primitiveNamingRules.Schemas[0].Tables[3].Columns.Count.ShouldBe(1);
        primitiveNamingRules.Schemas[0].Tables[4].Columns.Count.ShouldBe(1);
        primitiveNamingRules.Schemas[0].Tables[0].Columns[0].PropertyName.ShouldBe("ProductId");
        primitiveNamingRules.Schemas[0].Tables[0].Columns[0].NewName.ShouldBe("ProductID");
        primitiveNamingRules.Schemas[0].Tables.ForEach(o => (o.EntityName?.IsValidSymbolName() != false).ShouldBeTrue());
        primitiveNamingRules.Schemas[0].Tables.ForEach(o => o.NewName.IsValidSymbolName().ShouldBeTrue());
        primitiveNamingRules.Schemas[0].Tables.SelectMany(o => o.Columns)
            .ForAll(o => (o.PropertyName?.IsValidSymbolName() != false).ShouldBeTrue());
        primitiveNamingRules.Schemas[0].Tables.SelectMany(o => o.Columns)
            .ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());

        navigationNamingRules.Namespace.ShouldBe("");
        navigationNamingRules.Classes.Count.ShouldBe(9);
        navigationNamingRules.Classes.All(o => o.Properties.Count > 0).ShouldBeTrue();
        navigationNamingRules.Classes[0].Properties[0].Name.Contains("Orders").ShouldBeTrue();
        //navigationNamingRules.Classes[0].Properties[0].Name.Contains("Products").ShouldBeTrue();
        navigationNamingRules.Classes[0].Properties[0].NewName.ShouldBe("OrdersCustom");
        navigationNamingRules.Classes.ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        navigationNamingRules.Classes.SelectMany(o => o.Properties)
            .ForAll(o => o.Name.ForAll(n => n.IsValidSymbolName().ShouldBeTrue()));
        navigationNamingRules.Classes.SelectMany(o => o.Properties)
            .ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());

        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Rule contents look good at {elapsed}ms");

        var csProj = ResolveNorthwindProject();
        var projBasePath = new FileInfo(csProj).Directory!.FullName;
        var applicator = new RuleApplicator(projBasePath);
        applicator.OnLog += LogReceived;
        start = DateTimeExtensions.GetTime();
        ApplyRulesResponse response;
        response = await applicator.ApplyRules(primitiveNamingRules);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count() == 1) response.Errors.First().ShouldStartWith("Error loading existing project");

        response.Information.Last().ShouldStartWith("16 classes renamed, 46 properties renamed across 29 files", Case.Insensitive);
        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Primitive naming rules applied correctly at {elapsed}ms");

        response = await applicator.ApplyRules(navigationNamingRules);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count() == 1) response.Errors.First().ShouldStartWith("Error loading existing project");

        var renamed = response.Information.Where(o => o.StartsWith("Renamed")).ToArray();
        renamed.Length.ShouldBe(14);
        var couldNotFind = response.Information.Where(o => o.StartsWith("Could not find ")).ToArray();
        couldNotFind.Length.ShouldBe(0);
        response.Information.Last().ShouldStartWith("14 properties renamed across 10 files", Case.Insensitive);

        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Navigation naming rules applied correctly at {elapsed}ms");

        response = await applicator.ApplyRules(enumMappingRules);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count() == 1) response.Errors.First().ShouldStartWith("Error loading existing project");

        response.Information.Count(o => o.StartsWith("Update")).ShouldBe(2);
        response.Information.Last().ShouldStartWith("2 property types changed across 2 files", Case.Insensitive);

        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Enum mapping rules applied correctly at {elapsed}ms");
        logReceivedCount.ShouldBeGreaterThan(30);
        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Completed in {elapsed}ms.");
#if DEBUG
        output.WriteLine(
            $"FindClassesByNameTime: {RoslynExtensions.FindClassesByNameTime}ms.  RenameClassAsyncTime: {RoslynExtensions.RenameClassAsyncTime}ms.  RenamePropertyAsyncTime: {RoslynExtensions.RenamePropertyAsyncTime}ms.  ChangePropertyTypeAsyncTime: {RoslynExtensions.ChangePropertyTypeAsyncTime}ms");
#endif

        void LogReceived(object sender, LogMessage logMessage) {
            logReceivedCount++;
        }
    }

    [Fact]
    public async Task TypeFinder() {
        string projectBasePath = ResolveNorthwindProject();
        var state = new RuleApplicator.RoslynProjectState(new RuleApplicator(projectBasePath));
        var response = new ApplyRulesResponse(null);
        await state.TryLoadProjectOrFallbackOnce(projectBasePath, null, null, response);
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
        var path = Path.Combine(dir.FullName, "NorthwindTestEdmx\\Northwind.edmx");
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