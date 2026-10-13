using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using EntityFrameworkRuler.Design.Scaffolding;
using EntityFrameworkRuler.Design.Services;
using EntityFrameworkRuler.Design.Services.Models;
using EntityFrameworkRuler.Extension;
using EntityFrameworkRuler.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace EntityFrameworkRuler.Design.Tests;

/// <summary>
/// Runs the ruled scaffolding factory against an in-memory database model and in-memory rules (no file or database IO),
/// asserting that inheritance hierarchies keep their relationships regardless of rule declaration order.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class InheritanceScaffoldingTests {
    public static IEnumerable<object[]> TptDeclarationOrders => new[] {
        // Lens, Core and Tool declared first: the previous ordering visited Core before Tool and dropped FKs to Tool.
        new object[] { new[] { "LensDesign", "Core", "Tool", "PartDesign", "Assembly", "BaseToolDefinition", "PartNote" } },
        new object[] { new[] { "Assembly", "BaseToolDefinition", "Core", "LensDesign", "PartDesign", "PartNote", "Tool" } }
    };

    public static IEnumerable<object[]> TphDeclarationOrders => new[] {
        new object[] { new[] { "RecipeArcFillet", "RecipeLine", "RecipeArc", "RecipeZone", "Region", "Tracker" } },
        new object[] { new[] { "Region", "Tracker", "RecipeZone", "RecipeArc", "RecipeArcFillet", "RecipeLine" } }
    };

    [Theory]
    [MemberData(nameof(TptDeclarationOrders))]
    public void Tpt_derived_principals_keep_keys_foreign_keys_and_navigations(string[] declarationOrder) {
        var model = Scaffold(TptDatabase(), Context(declarationOrder.Select(TptEntity)), out var rules);

        var partDesign = EntityType(model, "PartDesign");
        partDesign.BaseType.ShouldBeNull();
        partDesign.GetMappingStrategy().ShouldBe("TPT");
        ShouldInherit(model, "Tool", "PartDesign", "Tool");
        ShouldInherit(model, "Core", "Tool", "Core");
        ShouldInherit(model, "LensDesign", "PartDesign", "Lens");
        foreach (var derived in new[] { "Tool", "Core", "LensDesign" }) {
            var entity = EntityType(model, derived);
            entity.FindDeclaredProperty("PartID").ShouldBeNull($"{derived} redeclares the inherited key");
            (entity.FindProperty("PartID")?.DeclaringType.Name).ShouldBe("PartDesign");
        }

        ShouldHaveForeignKey(model, "FK_BaseToolDefinition_Tool", "BaseToolDefinition", "PartID", "Tool", "Tool", "BaseToolDefinition",
            unique: true, required: true);
        ShouldHaveForeignKey(model, "FK_Assembly_FrontInsert", "Assembly", "FrontInsertID", "Tool", "FrontInsert", "FrontInsertAssemblies",
            unique: false, required: false);
        ShouldHaveForeignKey(model, "FK_PartNote_PartDesign", "PartNote", "PartID", "PartDesign", "Part", "PartNotes",
            unique: false, required: true);

        ShouldStillMap(rules, "PartDesign", "PartID", "UserDefinedName");
    }

    [Theory]
    [MemberData(nameof(TphDeclarationOrders))]
    public void Same_table_tph_hierarchy_keeps_incoming_and_outgoing_foreign_keys(string[] declarationOrder) {
        var model = Scaffold(TphDatabase(), Context(declarationOrder.Select(TphEntity)), out var rules);

        var root = EntityType(model, "RecipeZone");
        root.BaseType.ShouldBeNull();
        root.GetTableName().ShouldBe("Recipe");
        root.GetMappingStrategy().ShouldBe("TPH");
        (root.FindDiscriminatorProperty()?.Name).ShouldBe("RecipeClassName");
        ShouldInherit(model, "RecipeArc", "RecipeZone", "Recipe");
        ShouldInherit(model, "RecipeLine", "RecipeZone", "Recipe");
        ShouldInherit(model, "RecipeArcFillet", "RecipeArc", "Recipe");
        foreach (var derived in new[] { "RecipeArc", "RecipeLine", "RecipeArcFillet" }) {
            var entity = EntityType(model, derived);
            entity.GetDeclaredProperties().ShouldBeEmpty($"{derived} redeclares base columns");
            entity.GetDiscriminatorValue().ShouldBe(derived);
        }

        ShouldHaveForeignKey(model, "FK_Recipe_Region", "RecipeZone", "RegionID", "Region", "Region", "Recipes",
            unique: false, required: true);
        ShouldHaveForeignKey(model, "FK_Tracker_Recipe", "Tracker", "RecipeID", "RecipeZone", "Recipe", "Trackers",
            unique: false, required: true);
        ShouldHaveForeignKey(model, "FK_Tracker_ChildRecipe", "Tracker", "ChildRecipeID", "RecipeZone", "ChildRecipe", "ChildTrackers",
            unique: false, required: false);

        ShouldStillMap(rules, "RecipeZone", "RecipeID", "RegionID", "RecipeClassName");
    }

    private static IModel Scaffold(DatabaseModel database, DbContextRule rule, out DbContextRuleNode rules) {
        rules = new DbContextRuleNode(rule);
        var reverseEngineerOptions = new ModelReverseEngineerOptions();
        var loader = new Mock<IDesignTimeRuleLoader>();
        loader.Setup(o => o.GetDbContextRules()).Returns(rules);
        loader.SetupGet(o => o.CodeGenOptions).Returns(new ModelCodeGenerationOptionsEx {
            ContextName = rule.Name, ContextNamespace = "Inheritance", ModelNamespace = "Inheritance", SuppressOnConfiguring = true
        });
        loader.SetupGet(o => o.ReverseEngineerOptions).Returns(reverseEngineerOptions);
        loader.SetupGet(o => o.EfVersion).Returns(typeof(ModelCodeGenerationOptions).Assembly.GetName().Version);
        loader.SetupGet(o => o.TargetAssemblies).Returns(Array.Empty<Assembly>());

        // Same composition order as EF design-time tooling: EF design services, provider services, then Ruler services.
        var services = new ServiceCollection();
        services.AddEntityFrameworkDesignTimeServices();
        new SqlServerDesignTimeServices().ConfigureDesignTimeServices(services);
        new RuledDesignTimeServices().ConfigureDesignTimeServices(services);
        services.AddSingleton(loader.Object);
        services.AddSingleton(new Mock<IRuleModelUpdater>().Object);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IScaffoldingModelFactory>();
        factory.ShouldBeOfType<RuledRelationalScaffoldingModelFactory>();
        return factory.Create(database, reverseEngineerOptions);
    }

    private static void ShouldInherit(IModel model, string name, string baseName, string table) {
        var entity = EntityType(model, name);
        (entity.BaseType?.Name).ShouldBe(baseName, $"{name} base type");
        entity.GetTableName().ShouldBe(table, $"{name} table");
    }

    private static void ShouldHaveForeignKey(IModel model, string constraint, string dependent, string column, string principal,
        string dependentNavigation, string principalNavigation, bool unique, bool required) {
        var fk = model.GetEntityTypes().SelectMany(o => o.GetDeclaredForeignKeys()).SingleOrDefault(o => o.GetConstraintName() == constraint);
        fk.ShouldNotBeNull($"{constraint} missing");
        fk.DeclaringEntityType.Name.ShouldBe(dependent, constraint);
        fk.Properties.Select(o => o.Name).ShouldBe(new[] { column }, false, constraint);
        fk.Properties.ShouldAllBe(o => !o.IsShadowProperty(), constraint);
        fk.PrincipalEntityType.Name.ShouldBe(principal, constraint);
        fk.PrincipalKey.IsPrimaryKey().ShouldBeTrue(constraint);
        fk.IsUnique.ShouldBe(unique, constraint);
        fk.IsRequired.ShouldBe(required, constraint);
        (fk.DependentToPrincipal?.Name).ShouldBe(dependentNavigation, constraint);
        (fk.PrincipalToDependent?.Name).ShouldBe(principalNavigation, constraint);
    }

    // A derived entity must never omit (and so disable) a property rule owned by its base.
    private static void ShouldStillMap(DbContextRuleNode rules, string entity, params string[] columns) {
        var node = rules.Schemas.Single().Entities.Single(o => o.Rule.NewName == entity);
        foreach (var column in columns)
            (node.FindPropertyByColumn(column)?.ShouldMap()).ShouldBe(true, $"{entity}.{column} rule");
    }

    private static IEntityType EntityType(IModel model, string name)
        => model.FindEntityType(name) ?? throw new ShouldAssertException($"Entity {name} missing");

    private static EntityRule TptEntity(string name) => name switch {
        "PartDesign" => Entity("PartDesign", name, null, new[] { "PartID", "UserDefinedName" },
            Nav("PartNotes", "FK_PartNote_PartDesign", "PartNote", true, "*")).UseTptMapping(),
        "Tool" => Entity("Tool", name, "PartDesign", new[] { "Revision" },
            Nav("BaseToolDefinition", "FK_BaseToolDefinition_Tool", "BaseToolDefinition", true, "1"),
            Nav("FrontInsertAssemblies", "FK_Assembly_FrontInsert", "Assembly", true, "*")),
        "Core" => Entity("Core", name, "Tool", new[] { "IsVariational" }),
        "LensDesign" => Entity("Lens", name, "PartDesign", new[] { "Power" }),
        "BaseToolDefinition" => Entity("BaseToolDefinition", name, null, new[] { "PartID", "Name" },
            Nav("Tool", "FK_BaseToolDefinition_Tool", "Tool", false, "1")),
        "Assembly" => Entity("Assembly", name, null, new[] { "AssemblyID", "FrontInsertID" },
            Nav("FrontInsert", "FK_Assembly_FrontInsert", "Tool", false, "0..1")),
        "PartNote" => Entity("PartNote", name, null, new[] { "NoteID", "PartID" },
            Nav("Part", "FK_PartNote_PartDesign", "PartDesign", false, "1")),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static EntityRule TphEntity(string name) {
        switch (name) {
            case "RecipeZone":
                var root = Entity("Recipe", name, null, new[] { "RecipeID", "RegionID", "RecipeClassName" },
                        Nav("Region", "FK_Recipe_Region", "Region", false, "1"),
                        Nav("Trackers", "FK_Tracker_Recipe", "Tracker", true, "*"),
                        Nav("ChildTrackers", "FK_Tracker_ChildRecipe", "Tracker", true, "*"))
                    .UseTphMapping().SetDiscriminatorColumn("RecipeClassName");
                var discriminator = root.Properties.Single(o => o.Name == "RecipeClassName");
                foreach (var leaf in new[] { "RecipeArc", "RecipeArcFillet", "RecipeLine" })
                    discriminator.DiscriminatorConditions.Add(new() { Value = leaf, ToEntityName = leaf });
                return root;
            case "RecipeArc":
            case "RecipeLine":
                return Entity("Recipe", name, "RecipeZone", Array.Empty<string>());
            case "RecipeArcFillet":
                return Entity("Recipe", name, "RecipeArc", Array.Empty<string>());
            case "Region":
                return Entity("Region", name, null, new[] { "RegionID", "Name" },
                    Nav("Recipes", "FK_Recipe_Region", "RecipeZone", true, "*"));
            case "Tracker":
                return Entity("Tracker", name, null, new[] { "TrackerID", "RecipeID", "ChildRecipeID" },
                    Nav("Recipe", "FK_Tracker_Recipe", "RecipeZone", false, "1"),
                    Nav("ChildRecipe", "FK_Tracker_ChildRecipe", "RecipeZone", false, "0..1"));
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static EntityRule Entity(string table, string name, string baseType, string[] columns, params NavigationRule[] navigations) {
        var rule = new EntityRule { Name = table, NewName = name, BaseTypeName = baseType };
        foreach (var column in columns) rule.Properties.Add(new() { Name = column, NewName = column });
        foreach (var navigation in navigations) rule.Navigations.Add(navigation);
        return rule;
    }

    private static NavigationRule Nav(string name, string fkName, string toEntity, bool isPrincipal, string multiplicity)
        => new() { NewName = name, FkName = fkName, ToEntity = toEntity, IsPrincipal = isPrincipal, Multiplicity = multiplicity };

    private static DbContextRule Context(IEnumerable<EntityRule> entities) {
        var schema = new SchemaRule { SchemaName = "dbo" };
        foreach (var entity in entities) schema.Entities.Add(entity);
        var context = new DbContextRule { Name = "InheritanceContext" };
        context.Schemas.Add(schema);
        return context;
    }

    private static DatabaseModel TptDatabase() {
        var db = new DatabaseModel { DatabaseName = "inheritance", DefaultSchema = "dbo" };
        var partDesign = Table(db, "PartDesign", "PartID");
        Column(partDesign, "UserDefinedName", "varchar(50)", false);

        var tool = Table(db, "Tool", "PartID");
        Column(tool, "Revision", "varchar(3)", false);
        ForeignKey("FK_Tool_PartDesign", Key(tool), partDesign, ReferentialAction.Cascade);

        var core = Table(db, "Core", "PartID");
        Column(core, "IsVariational", "bit", false);
        ForeignKey("FK_Core_Tool", Key(core), tool, ReferentialAction.Cascade);

        var lens = Table(db, "Lens", "PartID");
        Column(lens, "Power", "float", true);
        ForeignKey("FK_Lens_PartDesign", Key(lens), partDesign, ReferentialAction.Cascade);

        var definition = Table(db, "BaseToolDefinition", "PartID"); // shared-key dependent of derived Tool
        Column(definition, "Name", "varchar(50)", false);
        ForeignKey("FK_BaseToolDefinition_Tool", Key(definition), tool, ReferentialAction.Cascade);

        var assembly = Table(db, "Assembly", "AssemblyID"); // ordinary FK to derived Tool
        ForeignKey("FK_Assembly_FrontInsert", Column(assembly, "FrontInsertID", "int", true), tool);

        var note = Table(db, "PartNote", "NoteID"); // control: FK to the hierarchy root
        ForeignKey("FK_PartNote_PartDesign", Column(note, "PartID", "int", false), partDesign);
        return db;
    }

    private static DatabaseModel TphDatabase() {
        var db = new DatabaseModel { DatabaseName = "inheritance", DefaultSchema = "dbo" };
        var region = Table(db, "Region", "RegionID");
        Column(region, "Name", "varchar(50)", false);

        var recipe = Table(db, "Recipe", "RecipeID");
        ForeignKey("FK_Recipe_Region", Column(recipe, "RegionID", "int", false), region);
        Column(recipe, "RecipeClassName", "varchar(100)", false);

        var tracker = Table(db, "Tracker", "TrackerID");
        ForeignKey("FK_Tracker_Recipe", Column(tracker, "RecipeID", "int", false), recipe);
        ForeignKey("FK_Tracker_ChildRecipe", Column(tracker, "ChildRecipeID", "int", true), recipe);
        return db;
    }

    private static DatabaseTable Table(DatabaseModel db, string name, string keyColumn) {
        var table = new DatabaseTable { Database = db, Name = name, Schema = "dbo" };
        db.Tables.Add(table);
        table.PrimaryKey = new DatabasePrimaryKey { Table = table, Name = "PK_" + name };
        table.PrimaryKey.Columns.Add(Column(table, keyColumn, "int", false));
        return table;
    }

    private static DatabaseColumn Column(DatabaseTable table, string name, string storeType, bool nullable) {
        var column = new DatabaseColumn { Table = table, Name = name, StoreType = storeType, IsNullable = nullable };
        table.Columns.Add(column);
        return column;
    }

    private static DatabaseColumn Key(DatabaseTable table) => table.PrimaryKey.Columns[0];

    private static void ForeignKey(string name, DatabaseColumn column, DatabaseTable principal,
        ReferentialAction onDelete = ReferentialAction.NoAction) {
        var fk = new DatabaseForeignKey { Table = column.Table, Name = name, PrincipalTable = principal, OnDelete = onDelete };
        fk.Columns.Add(column);
        fk.PrincipalColumns.Add(Key(principal));
        column.Table.ForeignKeys.Add(fk);
    }
}
