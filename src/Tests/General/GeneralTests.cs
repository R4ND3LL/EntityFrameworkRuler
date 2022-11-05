using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EdmxRuler.Extensions;
using EdmxRuler.Generator;
using EdmxRuler.RuleModels.EnumMapping;
using EdmxRuler.RuleModels.PropertyRenaming;
using EdmxRuler.RuleModels.TableColumnRenaming;
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
    public void Test() {
        var edmxPath = ResolveNorthwindEdmxPath();

        var start = DateTimeExtensions.GetTime();
        var edmxProcessor = new EdmxRuleGenerator(edmxPath);
        var rules = edmxProcessor.TryGenerateRules();
        var elapsed = DateTimeExtensions.GetTime() - start;
        edmxProcessor.Errors.Count.ShouldBe(0);
        edmxProcessor.Rules.Count.ShouldBe(3);
        output.WriteLine($"Successfully generated {edmxProcessor.Rules.Count} rule files in {elapsed}ms");
        rules.ShouldBe(edmxProcessor.Rules);
        var enumRules = rules.OfType<EnumMappingRulesRoot>().Single();
        var tableAndColumnRules = rules.OfType<TableAndColumnRulesRoot>().Single();
        var classPropertyNamingRules = rules.OfType<ClassPropertyNamingRulesRoot>().Single();

        enumRules.Classes.Count.ShouldBe(2);
        enumRules.Classes[0].Name.ShouldBe("Orders");
        enumRules.Classes[1].Name.ShouldBe("Products");
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

        classPropertyNamingRules.Classes.Count.ShouldBe(10);
        classPropertyNamingRules.Classes.All(o => o.Properties.Count > 0).ShouldBeTrue();
        classPropertyNamingRules.Classes[0].Properties[0].Name.ShouldBe("ProductCategoryIDNavigations");
        classPropertyNamingRules.Classes[0].Properties[0].AlternateName.ShouldBe("Products");
        classPropertyNamingRules.Classes[0].Properties[0].NewName.ShouldBe("Products");
        output.WriteLine($"Rule contents look good");
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

    private void Log(string msg) {
        output.WriteLine(msg);
        Debug.WriteLine(msg);
    }
}