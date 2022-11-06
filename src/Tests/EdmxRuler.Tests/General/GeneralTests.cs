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
    public async Task Test() {
        var edmxPath = ResolveNorthwindEdmxPath();

        var start = DateTimeExtensions.GetTime();
        var edmxProcessor = new RuleGenerator(edmxPath);
        var rules = edmxProcessor.TryGenerateRules();
        var elapsed = DateTimeExtensions.GetTime() - start;
        edmxProcessor.Errors.Count.ShouldBe(0);
        edmxProcessor.Rules.Count.ShouldBe(3);
        output.WriteLine($"Successfully generated {edmxProcessor.Rules.Count} rule files in {elapsed}ms");
        rules.ShouldBe(edmxProcessor.Rules);
        var enumRules = rules.OfType<EnumMappingRulesRoot>().Single();
        var tableAndColumnRules = rules.OfType<PrimitiveNamingRules>().Single();
        var classPropertyNamingRules = rules.OfType<NavigationNamingRules>().Single();

        enumRules.Classes.ForEach(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        enumRules.Classes.SelectMany(o => o.Properties).ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        enumRules.Classes.Count.ShouldBe(2);
        enumRules.Classes[0].Name.ShouldBe("Order");
        enumRules.Classes[1].Name.ShouldBe("Product");
        enumRules.Classes[0].Properties.Count.ShouldBe(1);
        enumRules.Classes[1].Properties.Count.ShouldBe(1);
        enumRules.Classes[0].Properties[0].Name.ShouldBe("Freight");
        enumRules.Classes[0].Properties[0].EnumType.ShouldBe("NorthwindModel.FreightEnum"); // internal type
        enumRules.Classes[1].Properties[0].Name.ShouldBe("ReorderLevel");
        enumRules.Classes[1].Properties[0].EnumType.ShouldBe("Common.OrderLevelEnum"); // external type

        tableAndColumnRules.Schemas.Count.ShouldBe(1);
        tableAndColumnRules.Schemas[0].Tables.Count.ShouldBe(10);
        tableAndColumnRules.Schemas[0].Tables[0].Columns.Count.ShouldBe(0);
        tableAndColumnRules.Schemas[0].Tables[3].Columns.Count.ShouldBe(1);
        tableAndColumnRules.Schemas[0].Tables[4].Columns.Count.ShouldBe(1);
        tableAndColumnRules.Schemas[0].Tables[3].Columns[0].Name.ShouldBe("ReportsTo");
        tableAndColumnRules.Schemas[0].Tables[3].Columns[0].NewName.ShouldBe("ReportsToFk");
        tableAndColumnRules.Schemas[0].Tables.ForEach(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        tableAndColumnRules.Schemas[0].Tables.ForEach(o => o.NewName.IsValidSymbolName().ShouldBeTrue());
        tableAndColumnRules.Schemas[0].Tables.SelectMany(o => o.Columns).ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        tableAndColumnRules.Schemas[0].Tables.SelectMany(o => o.Columns).ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());

        classPropertyNamingRules.Classes.Count.ShouldBe(10);
        classPropertyNamingRules.Classes.All(o => o.Properties.Count > 0).ShouldBeTrue();
        classPropertyNamingRules.Classes[0].Properties[0].Name.ShouldBe("ProductCategoryIDNavigations");
        classPropertyNamingRules.Classes[0].Properties[0].AlternateName.ShouldBe("Products");
        classPropertyNamingRules.Classes[0].Properties[0].NewName.ShouldBe("Products");
        classPropertyNamingRules.Classes.ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        classPropertyNamingRules.Classes.SelectMany(o => o.Properties).ForAll(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        classPropertyNamingRules.Classes.SelectMany(o => o.Properties).ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());

        output.WriteLine($"Rule contents look good");

        var csProj = ResolveNorthwindProject();
        var projBasePath = new FileInfo(csProj).Directory!.FullName;
        var applicator = new RuleApplicator(projBasePath);
        var response = await applicator.ApplyRules(classPropertyNamingRules);
        response.Errors.Count().ShouldBe(0);
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