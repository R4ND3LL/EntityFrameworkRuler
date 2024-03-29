﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkRuler.Applicator;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Extension;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Loader;
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
        var generator = new RuleGenerator();
        generator.Log += LogReceived;
        var generateRulesResponse = generator.GenerateRules(new GeneratorOptions(edmxPath));
        var rules = generateRulesResponse.Rules;
        var elapsed = DateTimeExtensions.GetTime() - start;
        generateRulesResponse.Errors.Count().ShouldBe(0);
        generateRulesResponse.Rules.Count.ShouldBe(1);
        output.WriteLine(
            $"Successfully generated {generateRulesResponse.Rules.Count} rule file{(generateRulesResponse.Rules.Count > 1 ? "s" : string.Empty)} in {elapsed}ms");
        rules.ShouldBe(generateRulesResponse.Rules);
        var enumMappingRules = rules.OfType<DbContextRule>().Single().Schemas.SelectMany(o => o.Entities).SelectMany(o => o.Properties)
            .Where(o => o.NewType.HasNonWhiteSpace()).ToList();
        var dbContextRule = rules.OfType<DbContextRule>().Single();
        var navigationNamingRules = rules.OfType<DbContextRule>().Single().Schemas
            .SelectMany(o => o.Entities)
            .SelectMany(o => o.Navigations)
            .ToList();

        enumMappingRules.Count.ShouldBe(2);
        enumMappingRules.ForEach(o => o.Name.IsValidSymbolName().ShouldBeTrue());
        enumMappingRules.ForAll(o => (o.PropertyName == null || o.PropertyName.IsValidSymbolName()).ShouldBeTrue());

        dbContextRule.Schemas.Count.ShouldBe(1);
        dbContextRule.Schemas[0].Entities.Count.ShouldBe(35);
        var prod = dbContextRule.Schemas[0].Entities.FirstOrDefault(o => o.Name == "Products");
        prod.ShouldNotBeNull();
        prod.Properties.Count.ShouldBe(10);
        prod.Properties[0].PropertyName.ShouldBe("ProductId");
        prod.Properties[0].NewName.ShouldBe("ProductID");
        dbContextRule.Schemas[0].Entities.ForAll(o => (o.EntityName?.IsValidSymbolName() != false).ShouldBeTrue());
        dbContextRule.Schemas[0].Entities.ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());
        dbContextRule.Schemas[0].Entities.SelectMany(o => o.Properties)
            .ForAll(o => (o.PropertyName?.IsValidSymbolName() != false).ShouldBeTrue());
        dbContextRule.Schemas[0].Entities.SelectMany(o => o.Properties)
            .ForAll(o => (o.NewName == null || o.NewName.IsValidSymbolName()).ShouldBeTrue());

        navigationNamingRules.Count.ShouldBeGreaterThan(15);
        var fkOrdersCustomersRules = navigationNamingRules.Where(o => o.FkName == "FK_Orders_Customers").ToArray();
        fkOrdersCustomersRules.Length.ShouldBe(2);
        fkOrdersCustomersRules.Any(o => o.Name.Contains("Orders")).ShouldBeTrue();
        fkOrdersCustomersRules.Any(o => o.Name.Contains("CustomerNavigation")).ShouldBeTrue();
        fkOrdersCustomersRules.Any(o => o.NewName == "OrdersCustom").ShouldBeTrue();

        navigationNamingRules.ForAll(o => o.NewName.IsValidSymbolName().ShouldBeTrue());
        navigationNamingRules.ForAll(o => (o.Name.IsNullOrEmpty() || o.Name.IsValidSymbolName()).ShouldBeTrue());

        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Rule contents look good at {elapsed}ms");

        var csProj = ResolveNorthwindTestRoslynProject();
        var projBasePath = new FileInfo(csProj).Directory!.FullName;
        IRuleApplicator applicator = new RuleApplicator();
        applicator.Log += LogReceived;
        start = DateTimeExtensions.GetTime();
        var responses = await applicator.ApplyRules(projBasePath, adhocOnly: true, dbContextRule);
        var response = responses.First();
        response.Errors.Count().ShouldBeLessThanOrEqualTo(1);
        if (response.Errors.Count() == 1) response.Errors.First().ShouldStartWith("Error loading existing project");

        response.Information.Last()
            .ShouldStartWith("16 classes renamed, 60 properties renamed, 2 property types changed across 2", Case.Insensitive);
        elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"DbContext rules applied correctly at {elapsed}ms");

        var renamed = response.Information.Where(o => o.StartsWith("Renamed")).ToArray();
        renamed.Length.ShouldBeGreaterThan(70);
        var couldNotFind = response.Information.Where(o => o.StartsWith("Could not find ") && !o.Contains("Sysdiagram")).ToArray();
        couldNotFind.Length.ShouldBe(10);
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
        var projectBasePath = ResolveNorthwindTestRoslynProject();
        var state = new RuleApplicator.RoslynProjectState(new RuleApplicator());
        var response = new ApplyRulesResponse(null, NullMessageLogger.Instance);
        await state.TryLoadProjectOrFallbackOnce(new ApplicatorOptions(projectBasePath, true), response);
        var project = state.Project;
        project.ShouldNotBeNull();
        //var ns = "NorthwindTestRoslyn.Models";
        var result = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindDeclarationsAsync(project, "Product",
            false,
            SymbolFilter.Type, CancellationToken.None);
        var results = result.Where(o => o.Kind == SymbolKind.NamedType)
            .OfType<ITypeSymbol>().Where(o => o.TypeKind == TypeKind.Class && !o.IsAnonymousType && !o.IsValueType)
            .ToList();
        results.Count.ShouldBe(1);

        var compilation = await project.GetCompilationAsync();
        compilation.ShouldNotBeNull();
        var type = compilation.GetTypeByMetadataName("NorthwindModel.Models.Product");
        var syntaxReferences2 = type?.DeclaringSyntaxReferences;
        syntaxReferences2.ShouldNotBeNull();
    }

    [Fact]
    public async Task ShouldLoadRules() {
        var start = DateTimeExtensions.GetTime();
        IRuleApplicator ruleApplicator = new RuleApplicator();
        var rules = await ruleApplicator.LoadRulesInProjectPath(NorthwindTestDesignProject());
        rules.ShouldNotBeNull();
        rules.Rules.ShouldNotBeNull();
        rules.Rules.Count.ShouldBeGreaterThan(0);
        var elapsed = DateTimeExtensions.GetTime() - start;
        output.WriteLine($"Loaded {rules.Rules.Count} in {elapsed}ms.");
    }

    [Fact]
    public void ShouldSerializeAnnotations() {
        var rule = new DbContextRule();
        rule.IncludeUnknownSchemas = true;
        var testAnnotation = "TEST_ANNOTATION";
        rule.Annotations.Add(testAnnotation, "123");
        var json = rule.WriteJson();
        json.ShouldContain(testAnnotation);
        var rule2 = json.ReadJson<DbContextRule>();
        rule2.Annotations.ContainsKey(testAnnotation).ShouldBeTrue();
    }

    [Fact]
    public void ShouldSerializeNavigationNameArray() {
        var json = $$"""
            {
                "Schemas": [
                    {
                        "SchemaName": "dbo",
                        "Entities": [
                            {
                                "Name": "TestEntity",
                                "Properties": [
                                    {
                                        "Name": "ContactTitle",
                                        "DiscriminatorConditions": [
                                            {
                                                "Value": "Red",
                                                "ToEntityName": "CustomerRed"
                                            },
                                            {
                                                "Value": "Green",
                                                "ToEntityName": "CustomerGreen"
                                            }
                                        ]
                                    }
                                ],
                                "Navigations": [
                                    {
                                        "Name": [
                                            "TestNav"
                                        ],
                                        "FkName": "Fk",
                                        "Annotations": {
                                            "Relational:MappingStrategy": "TPH",
                                            "Number": 45
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
            """;

        var rule = json.ReadJson<DbContextRule>();
        rule.Schemas[0].SchemaName.ShouldBe("dbo");
        rule.Schemas[0].Entities[0].Name.ShouldBe("TestEntity");
        rule.Schemas[0].Entities[0].Navigations[0].Name.ShouldBe("TestNav");
        rule.Schemas[0].Entities[0].Navigations[0].FkName.ShouldBe("Fk");
        rule.Schemas[0].Entities[0].Properties[0].DiscriminatorConditions.Count.ShouldBe(2);
        var json2 = rule.WriteJson();
        json2.ShouldContain("TestNav");
        var rule2 = json2.ReadJson<DbContextRule>();
        rule2.Schemas[0].Entities[0].Navigations[0].Name.ShouldBe("TestNav");
        rule2.Schemas[0].Entities[0].Navigations[0].Annotations["Relational:MappingStrategy"].ShouldBe("TPH");
        rule2.Schemas[0].Entities[0].Navigations[0].Annotations["Number"].ShouldBe((long)45);
        rule2.Schemas[0].Entities[0].Properties[0].DiscriminatorConditions.Count.ShouldBe(2);
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

    private static string ResolveNorthwindTestRoslynProject() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "Tests") dir = dir.Parent;

        dir.ShouldNotBeNull();
        var path = Path.Combine(dir.FullName, $"NorthwindTestRoslyn{Path.DirectorySeparatorChar}NorthwindTestRoslyn.csproj");
        File.Exists(path).ShouldBeTrue();
        return path;
    }

    private static string NorthwindTestDesignProject() {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "Tests") dir = dir.Parent;

        dir.ShouldNotBeNull();
        var path = Path.Combine(dir.FullName, $"NorthwindTestDesign{Path.DirectorySeparatorChar}NorthwindTestDesign.csproj");
        File.Exists(path).ShouldBeTrue();
        return path;
    }

    private static async Task SampleCode() {
        var edmxPath = ResolveNorthwindEdmxPath();
        var projectBasePath = ResolveNorthwindTestRoslynProject();
        {
            // Generate and save rules:
            var generator = new RuleGenerator();
            var response = generator.GenerateRules(edmxPath);
            if (response.Success)
                await generator.SaveRules(response.Rules.First(), projectBasePath);
        }
        {
            // Apply rules already in project path:
            var applicator = new RuleApplicator();
            var response = await applicator.ApplyRulesInProjectPath(projectBasePath);
        }
        {
            // More control over which rules are applied:
            var applicator = new RuleApplicator();
            var loadResponse = await applicator.LoadRulesInProjectPath(projectBasePath);
            var applyResponse = await applicator.ApplyRules(projectBasePath, loadResponse.Rules.First());
        }
        {
            // Customize rule file name:
            var generator = new RuleGenerator();
            var response = generator.GenerateRules(edmxPath);
            if (response.Success)
                await generator.SaveRules(projectBasePath, dbContextRulesFile: "DbContextRules.json", response.Rules.First());
        }
        {
            // Handle log activity:
            var applicator = new RuleApplicator();
            applicator.Log += (sender, message) => Console.WriteLine(message);
            var response = await applicator.ApplyRulesInProjectPath(projectBasePath);
        }
    }

    private void Log(string msg) {
        output.WriteLine(msg);
        Debug.WriteLine(msg);
    }
}