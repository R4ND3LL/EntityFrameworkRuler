using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Extensions;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Saver;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

#pragma warning disable CS1591

namespace EntityFrameworkRuler.Design.Services;

/// <inheritdoc />
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class RuleModelUpdater : IRuleModelUpdater {
    private readonly IDesignTimeRuleLoader ruleLoader;
    private readonly IRuleSaver ruleSaver;
    private readonly IOperationReporter reporter;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleModelUpdater(IDesignTimeRuleLoader ruleLoader, IRuleSaver ruleSaver, IOperationReporter reporter) {
        this.ruleLoader = ruleLoader;
        this.ruleSaver = ruleSaver;
        this.reporter = reporter;
    }

    /// <inheritdoc />
    public void OnModelCreated(IModel model) {
        if (ruleLoader == null) return;
        var contextRules = ruleLoader.GetDbContextRules();
        if (contextRules is null || ReferenceEquals(contextRules, DbContextRule.DefaultNoRulesFoundBehavior)) {
            contextRules = new() {
                IncludeUnknownSchemas = true
            };
        }

        var start = DateTimeExtensions.GetTime();
        var projectDir = ruleLoader.GetProjectDir();
        if (projectDir.IsNullOrWhiteSpace()) {
            projectDir = contextRules.FilePath.HasNonWhiteSpace() ? Path.GetDirectoryName(contextRules.FilePath) : null;
            if (projectDir.IsNullOrWhiteSpace()) return;
        }

        var contextName = ruleLoader.CodeGenOptions?.ContextName;
        contextRules.Name = contextName;

        //var dbName = model.GetDatabaseName();
        UpdateDbContextRule(model, contextRules);

        // initialize a rule file from the current reverse engineered model
        var saver = ruleSaver ?? new RuleSaver();
        var response = saver.SaveRules(projectDir, null, contextRules).GetAwaiter().GetResult();
        var elapsed = DateTimeExtensions.GetTime() - start;

        if (response.Errors.Any()) {
            reporter?.WriteError($"Failed to save rule file: {response.Errors.First()}");
        } else if (response.SavedRules.Count > 0) {
            var fn = Path.GetFileName(response.SavedRules[0]);
            reporter?.WriteInformation($"Updated {fn} in {elapsed}ms");
        }
    }

    protected virtual void UpdateDbContextRule(IModel model, DbContextRule contextRules) {
        var entitiesByName = model.GetEntityTypes().ToDictionary(o => o.Name);
        var entitiesBySchema = entitiesByName.Values
            .Select(ToEntityInfo)
            .GroupBy(o => o.Schema)
            .ToDictionary(o => o.Key,
                o => o
                    .Where(e => e.DbName?.Length > 0)
                    .ToDictionary(e => e.DbName));

        // update schemas
        foreach (var schemaRule in contextRules.Schemas) {
            var entitiesByDbName = entitiesBySchema.TryGetValue(schemaRule.SchemaName, out var x) ? x : null;
            if (entitiesByDbName == null) {
                // rule doesnt have a corresponding schema. can we update the rule to indicate that it is stale?
                continue;
            }

            // remove from list so that we can tell what needs to be added after this loop
            entitiesBySchema.Remove(schemaRule.SchemaName);

            foreach (var tableRule in schemaRule.Tables) {
                var table = entitiesByDbName?.TryGetValue(tableRule.Name);
                if (table?.DbName == null) {
                    // rule doesnt have a corresponding entity. can we update the rule to indicate that it is stale?
                    continue;
                }

                // remove from list so that we can tell what needs to be added after this loop
                entitiesByDbName.Remove(tableRule.Name);
                UpdateTableRule(tableRule, table.Value);
            }

            if (!(entitiesByDbName.Count > 0)) continue;

            // add generated entities that dont have a corresponding rule (remainder of list after identification pass).
            if (schemaRule.IncludeUnknownTables)
                AddTables(schemaRule, entitiesByDbName.Values.Where(o => !o.IsView));

            if (schemaRule.IncludeUnknownViews)
                AddTables(schemaRule, entitiesByDbName.Values.Where(o => o.IsView));
        }

        if (entitiesBySchema.Count <= 0) return;
        if (!contextRules.IncludeUnknownSchemas) return;

        // add missing schemas
        foreach (var entities in entitiesBySchema) {
            var schemaRule = new SchemaRule {
                SchemaName = entities.Key,
                IncludeUnknownTables = true,
                IncludeUnknownViews = true,
            };
            contextRules.Schemas.Add(schemaRule);
            foreach (var entity in entities.Value)
                schemaRule.Tables.Add(UpdateTableRule(null, entity.Value));
        }
    }


    private void AddTables(SchemaRule schemaRule, IEnumerable<EntityInfo> unknownTables) {
        if (unknownTables == null) return;
        foreach (var unknownTable in unknownTables) {
            schemaRule.Tables.Add(UpdateTableRule(null, unknownTable));
        }
    }

    private ColumnRule UpdateColumnRule(ColumnRule r, ColumnInfo property) {
        if (r == null) {
            var n = property.DbName;
            if (n == null) return null;
            r = new() { Name = n };
        }

        if (r.NewName.HasNonWhiteSpace() || r.Name != property.Property.Name) r.NewName = property.Property.Name;
        if (property.Property.ClrType?.IsEnum == true) r.NewType = property.Property.ClrType.FullName;
        return r;
    }


    private NavigationRule UpdateNavigationRule(NavigationRule r, NavigationInfo property) {
        if (r == null) {
            var n = property.FkName;
            if (n == null) return null;
            r = new() { FkName = n };
        }

        if (r.Name.Count == 0) r.Name.Add(property.Navigation.Name);
        r.NewName = property.Navigation.Name;
        r.ToEntity = property.ToEntity;
        r.IsPrincipal = property.IsPrincipal;
        r.Multiplicity = property.Multiplicity.ToMultiplicityString();
        return r;
    }

    private TableRule UpdateTableRule(TableRule r, EntityInfo entityInfo) {
        r ??= new() { Name = entityInfo.DbName, IncludeUnknownColumns = true };
        if (r.NewName.HasNonWhiteSpace() || r.Name != entityInfo.Entity.Name) r.NewName = entityInfo.Entity.Name;
        if (!r.IncludeUnknownColumns) return r;
        UpdateColumns(r, entityInfo);
        UpdateNavigations(r, entityInfo);
        return r;
    }

    private void UpdateNavigations(TableRule r, EntityInfo entityInfo) {
        // update nav rules
        var navsByDbName = entityInfo.Entity.GetNavigations().Select(ToNavigationInfo)
            .Where(o => o.FkName != null)
            .DistinctBy(o => (o.FkName, o.IsPrincipal)).ToDictionary(o => (o.FkName, o.IsPrincipal));
        foreach (var navigationRule in r.Navigations) {
            var key = (navigationRule.FkName, navigationRule.IsPrincipal);
            var info = navsByDbName.TryGetValue(key);
            if (info.FkName == null) {
                // rule doesnt have corresponding column. can we update the rule to indicate that it is stale?
                continue;
            }

            // remove from list so that we can tell what has is remaining after identification
            navsByDbName.Remove(key);
            UpdateNavigationRule(navigationRule, info);
        }

        if (navsByDbName.Count <= 0) return;

        // add generated properties that dont have corresponding rules (remainder of list after identification pass).
        foreach (var property in navsByDbName.Values)
            r.Navigations.Add(UpdateNavigationRule(null, property));
    }

    private void UpdateColumns(TableRule r, EntityInfo entityInfo) {
        // update column rules first
        var colsByDbName = entityInfo.Entity.GetProperties().Select(ToColumnInfo)
            .Where(o => o.DbName != null)
            .DistinctBy(o => o.DbName).ToDictionary(o => o.DbName);
        foreach (var columnRule in r.Columns) {
            var info = colsByDbName.TryGetValue(columnRule.Name);
            if (info.DbName == null) {
                // rule doesnt have corresponding column. can we update the rule to indicate that it is stale?
                continue;
            }

            // remove from list so that we can tell what has is remaining after identification
            colsByDbName.Remove(columnRule.Name);
            UpdateColumnRule(columnRule, info);
        }

        if (colsByDbName.Count <= 0) return;
        // add generated properties that dont have corresponding rules (remainder of list after identification pass).
        foreach (var property in colsByDbName.Values)
            r.Columns.Add(UpdateColumnRule(null, property));
    }


    private EntityInfo ToEntityInfo(IEntityType e) {
        var isView = e.IsView();
        var name = isView ? e.GetViewName() ?? e.GetTableName() : e.GetTableName() ?? e.GetViewName();
        var schema = (isView ? e.GetViewSchema() ?? e.GetSchema() : e.GetSchema() ?? e.GetViewSchema()) ??
                     e.Model.GetDefaultSchema() ?? string.Empty;
        return new(e, isView, schema, name);
    }

    private ColumnInfo ToColumnInfo(IProperty p) {
        var info = new ColumnInfo(p, p.GetColumnNameUsingStoreObject());
        return info;
    }

    private NavigationInfo ToNavigationInfo(INavigation n) {
        var info = new NavigationInfo(n, n.ForeignKey?.GetConstraintName(), !n.IsOnDependent, n.Inverse?.DeclaringEntityType?.Name,
            n.GetMultiplicity());
        return info;
    }
}

internal record struct EntityInfo(IEntityType Entity, bool IsView, string Schema, string DbName);

internal record struct ColumnInfo(IProperty Property, string DbName);

internal record struct NavigationInfo(INavigation Navigation, string FkName, bool IsPrincipal, string ToEntity, Multiplicity Multiplicity);

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public interface IRuleModelUpdater {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    void OnModelCreated(IModel model);
}