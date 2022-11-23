using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EntityFrameworkRuler.Rules;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Extension;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class RuleExtensions {
    /// <summary> Get the primitive schema rule for the given target schema. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static bool TryResolveRuleFor(this DbContextRule rules, string schema, out SchemaRule schemaRule) =>
        TryResolveRuleFor(rules?.Schemas, schema, out schemaRule);

    /// <summary> Get the primitive schema rule for the given target schema. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static SchemaRule TryResolveRuleFor(this DbContextRule rules, string schema) =>
        TryResolveRuleFor(rules?.Schemas, schema, out var schemaRule) ? schemaRule : null;

    /// <summary> Get the primitive schema rule for the given target schema. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static bool TryResolveRuleFor(this IList<SchemaRule> rules, string schema, out SchemaRule schemaRule) {
        schemaRule = null;
        if (rules.IsNullOrEmpty()) return false;

        if (schema.IsNullOrWhiteSpace()) {
            // default to dbo, otherwise fail
            if (rules.Count == 1 && (rules[0].Name.IsNullOrWhiteSpace() || rules[0].Name == "dbo")) schemaRule = rules[0];
            else return false;
        } else {
            schemaRule = rules.FirstOrDefault(x => x.Name == schema);
            // if there is only one schema, and the name is not defined, default to that
            if (schemaRule == null && rules.Count == 1 && rules[0].Name.IsNullOrWhiteSpace())
                schemaRule = rules[0];
        }

        return schemaRule != null;
    }

    /// <summary> Get the primitive table rule for the given target table. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static TableRule TryResolveRuleFor(this SchemaRule rules, string table)
        => TryResolveRuleFor(rules, table, out var tableRule) ? tableRule : null;

    /// <summary> Get the primitive table rule for the given target table. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static bool TryResolveRuleFor(this SchemaRule rules, string table, out TableRule tableRule) {
        tableRule = null;
        if (rules?.Tables == null || rules.Tables.Count == 0 || table.IsNullOrWhiteSpace()) return false;

        tableRule =
            rules.Tables?.FirstOrDefault(o => o.Name == table) ??
            rules.Tables?.FirstOrDefault(o => o.Name.IsNullOrWhiteSpace() && o.EntityName == table);

        return tableRule != null;
    }

    /// <summary> Get the primitive column rule for the given target column. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static ColumnRule TryResolveRuleFor(this TableRule rules, string column)
        => TryResolveRuleFor(rules, column, out var columnRule) ? columnRule : null;

    /// <summary> Get the primitive column rule for the given target column. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static bool TryResolveRuleFor(this TableRule rules, string column, out ColumnRule columnRule) {
        columnRule = null;
        if (rules?.Columns == null || rules.Columns.Count == 0 || column.IsNullOrWhiteSpace()) return false;

        columnRule =
            rules.Columns.FirstOrDefault(o => o.Name == column) ??
            rules.Columns.FirstOrDefault(o => o.Name.IsNullOrWhiteSpace() && o.PropertyName == column);

        return columnRule != null;
    }

    /// <summary> Get the primitive schema and table rules for the given target table. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static bool TryResolveRuleFor(this DbContextRule rules,
        string schema,
        string table,
        out SchemaRule schemaRule,
        out TableRule tableRule) {
        tableRule = null;
        if (!rules.TryResolveRuleFor(schema, out schemaRule)) return false;
        if (!schemaRule.TryResolveRuleFor(table, out tableRule)) return false;
        return tableRule != null;
    }

    /// <summary> Get the primitive schema, table and column rules for the given target column. Used during scaffolding phase. </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static bool TryResolveRuleFor(this DbContextRule rules,
        string schema,
        string table, string column,
        out SchemaRule schemaRule,
        out TableRule tableRule,
        out ColumnRule columnRule) {
        tableRule = null;
        columnRule = null;
        if (!rules.TryResolveRuleFor(schema, out schemaRule)) return false;
        if (!schemaRule.TryResolveRuleFor(table, out tableRule)) return false;
        if (!tableRule.TryResolveRuleFor(column, out columnRule)) return false;
        return columnRule != null;
    }

    /// <summary> Return the navigation naming rules TableRule object for the given entity </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static TableRule TryResolveRuleForEntity(this DbContextRule rules,
        string entity,
        string schema,
        string table) {
        if (rules?.Schemas == null || rules.Schemas.Count == 0 || entity.IsNullOrWhiteSpace()) return null;

        var schemas = rules.Schemas.Where(o => o.Tables?.Count > 0);
        // use optional schema filter
        if (schema.HasNonWhiteSpace())
            schemas = schemas.Where(o => o.Name.IsNullOrEmpty() || o.Name.EqualsIgnoreCase(schema));

        var tables = schemas.SelectMany(o => o.Tables);

        // use optional table filter
        // ReSharper disable once InvertIf
        if (table.HasNonWhiteSpace()) {
            tables = tables.Where(o => o.Name.IsNullOrEmpty() || o.Name.EqualsIgnoreCase(table));

            // this should be enough IF the rule had the DBName defined.
            // query now because this is more reliable than using the expected entity name.
            var tableRule = tables.FirstOrDefault(o => o.Name.HasNonWhiteSpace());
            if (tableRule != null) return tableRule;
        }

        return tables.FirstOrDefault(o => o.EntityName == entity);
    }

    /// <summary> Return the navigation naming rule for the given navigation info </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static NavigationRule TryResolveNavigationRuleFor(this TableRule tableRule,
        string fkName,
        Func<string> defaultEfName,
        bool thisIsPrincipal,
        bool isManyToMany) {
        if (tableRule?.Navigations == null || tableRule.Navigations.Count == 0) return null;

        // locate by fkName first, which is most reliable.
        var navigationRenames = fkName.HasNonWhiteSpace()
            ? tableRule.Navigations
                .Where(t => t.FkName == fkName)
                .ToArray()
            : Array.Empty<NavigationRule>();

        if (navigationRenames.Length == 0) {
            // Maybe FkName is not defined?  if not, try to locate by expected target name instead
            if (fkName.HasNonWhiteSpace()) {
                var someFkNamesEmpty = tableRule.Navigations.Any(o => o.FkName.IsNullOrWhiteSpace());
                if (!someFkNamesEmpty) {
                    // Fk names ARE defined, this property is just not found. Use default.
                    return null;
                }
            }

            var efName = defaultEfName();
            navigationRenames = tableRule.Navigations.Where(o => o.Name?.Count > 0 && o.Name.Contains(efName))
                .ToArray();
            if (navigationRenames.Length == 0) return null; // expected EF name resolution failed to
        }

        // we have candidate matches (by fk name or expected target name). we may need to narrow further.
        // ReSharper disable once InvertIf
        if (navigationRenames.Length > 1) {
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (isManyToMany)
                // many-to-many relationships always set IsPrincipal=true for both ends in the rule file.
                navigationRenames = navigationRenames.Where(o => o.IsPrincipal).ToArray();
            else
                // filter for this end only
                navigationRenames = navigationRenames.Where(o => o.IsPrincipal == thisIsPrincipal).ToArray();
        }

        return navigationRenames.Length != 1 ? null : navigationRenames[0];
    }

    /// <summary> Get the rule elements within this element </summary>
    public static IEnumerable<IRuleItem> GetChildren(this IRuleItem item) {
        return item switch {
            IRuleModelRoot r => r.GetSchemas(),
            ISchemaRule sr => sr.GetClasses(),
            IClassRule cr => cr.GetProperties(),
            _ => Enumerable.Empty<IRuleItem>()
        };
    }
    /// <summary> True if the rule element can contain child elements </summary>
    public static bool CanHaveChildren(this IRuleItem item) {
        return item switch {
            IRuleModelRoot => true,
            ISchemaRule => true,
            IClassRule => true,
            _ => false
        };
    }
}