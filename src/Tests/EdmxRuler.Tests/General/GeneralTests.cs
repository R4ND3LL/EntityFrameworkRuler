using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EdmxRuler.Applicator;
using EdmxRuler.Extensions;
using EdmxRuler.Generator;
using EdmxRuler.RuleModels.EnumMapping;
using EdmxRuler.RuleModels.NavigationNaming;
using EdmxRuler.RuleModels.PrimitiveNaming;
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

        var start = DateTimeExtensions.GetTime();
        var edmxProcessor = new RuleGenerator(edmxPath);
        var rules = edmxProcessor.TryGenerateRules();
        var elapsed = DateTimeExtensions.GetTime() - start;
        edmxProcessor.Errors.Count.ShouldBe(0);
        edmxProcessor.Rules.Count.ShouldBe(3);
        output.WriteLine($"Successfully generated {edmxProcessor.Rules.Count} rule files in {elapsed}ms");
        rules.ShouldBe(edmxProcessor.Rules);
        var enumMappingRules = rules.OfType<EnumMappingRules>().Single();
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
        enumMappingRules.Classes[0].Properties[0].EnumType
            .ShouldBe("NorthwindTestProject.Models.FreightEnum"); // internal type
        enumMappingRules.Classes[1].Properties[0].Name.ShouldBe("ReorderLevel");
        enumMappingRules.Classes[1].Properties[0].EnumType.ShouldBe("Common.OrderLevelEnum"); // external type

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

        navigationNamingRules.Namespace.ShouldBe("NorthwindTestProject.Models");
        navigationNamingRules.Classes.Count.ShouldBe(10);
        navigationNamingRules.Classes.All(o => o.Properties.Count > 0).ShouldBeTrue();
        navigationNamingRules.Classes[0].Properties[0].Name.ShouldBe("ProductCategoryIDNavigations");
        navigationNamingRules.Classes[0].Properties[0].AlternateName.ShouldBe("Products");
        navigationNamingRules.Classes[0].Properties[0].NewName.ShouldBe("Products");
        navigationNamingRules.Classes.ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        navigationNamingRules.Classes.SelectMany(o => o.Properties)
            .ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        navigationNamingRules.Classes.SelectMany(o => o.Properties)
            .ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());

        output.WriteLine($"Rule contents look good");

        var csProj = ResolveNorthwindProject();
        var projBasePath = new FileInfo(csProj).Directory!.FullName;
        var applicator = new RuleApplicator(projBasePath);
        var response = await applicator.ApplyRules(navigationNamingRules);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count == 1) {
            response.Errors[0].ShouldStartWith("Error loading existing project");
        }

        var renamed = response.Information.Where(o => o.StartsWith("Renamed")).ToArray();
        renamed.Length.ShouldBe(16);
        var couldNotFind = response.Information.Where(o => o.StartsWith("Could not find ")).ToArray();
        couldNotFind.Length.ShouldBe(2);
        response.Information.Last().ShouldContain("16 properties renamed across 11 files", Case.Insensitive);

        output.WriteLine($"Navigation naming rules applied correctly");

        response = await applicator.ApplyRules(enumMappingRules);
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count == 1) {
            response.Errors[0].ShouldStartWith("Error loading existing project");
        }

        response.Information.Count(o => o.StartsWith("Update")).ShouldBe(2);
        response.Information.Last().ShouldContain("2 properties mapped to enums across 2 files", Case.Insensitive);
        output.WriteLine($"Enum mapping rules applied correctly");
    }

    public static string ResolveNorthwindEdmxPath() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "src") {
            dir = dir.Parent;
        }

        dir.ShouldNotBeNull();
        var path = Path.Combine(dir.FullName, "Resources\\Northwind.edmx");
        File.Exists(path).ShouldBeTrue();
        return path;
    }

    public static string ResolveNorthwindProject() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "Tests") {
            dir = dir.Parent;
        }

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