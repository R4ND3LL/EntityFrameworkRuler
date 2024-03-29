﻿using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Design.Services.Models;

/// <inheritdoc />
public sealed class SchemaRuleNode : RuleNode<SchemaRule, DbContextRuleNode> {
    /// <inheritdoc />
    public SchemaRuleNode(SchemaRule r, DbContextRuleNode parent) : base(r, parent) {
        Entities = new(() => r.Entities.Select(o => new EntityRuleNode(o, this)), parent.Rule.CaseSensitive);
        Functions = new(() => r.Functions.Select(o => new FunctionRuleNode(o, this)), parent.Rule.CaseSensitive);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleIndex<EntityRuleNode> Entities { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleIndex<FunctionRuleNode> Functions { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public bool IsDynamicNamingTables =>
        Parent.Rule.PreserveCasingUsingRegex ||
        Rule.UseSchemaName ||
        (Rule.TableRegexPattern.HasNonWhiteSpace() && Rule.TablePatternReplaceWith != null);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public bool IsDynamicNamingColumns =>
        Parent.Rule.PreserveCasingUsingRegex ||
        Rule.UseSchemaName ||
        (Rule.ColumnRegexPattern.HasNonWhiteSpace() && Rule.ColumnPatternReplaceWith != null);

    /// <summary> Get the table rule for the given target table. Used during scaffolding phase. </summary>
    public ICollection<EntityRuleNode> TryResolveRuleForTable(string table) {
        if (Entities == null || Entities.Count == 0 || table.IsNullOrWhiteSpace()) return null;

        var entityRule = Entities?.GetByDbName(table);
        if (entityRule != null) {
            // scan is required to find all entities by the table name since there may be split tables
            var set = Entities.Where(o => o.DbName == table).ToSortedSet(EntitySizeComparer.Instance);
            Debug.Assert(set.Contains(entityRule));
            if (set.Contains(entityRule)) return set;
#if DEBUG
            Debugger.Break(); // the initially found entity should always be included
#endif
            set.Add(entityRule);
            return set;
        }

        entityRule = Entities?.GetByFinalName(table);
        if (entityRule?.DbName.HasNonWhiteSpace() == true) entityRule = null;

        return entityRule != null ? new[] { entityRule } : Array.Empty<EntityRuleNode>();
    }

    /// <summary> Get the dbFunction rule for the given target dbFunction name. Used during scaffolding phase. </summary>
    public FunctionRuleNode TryResolveRuleForFunction(string function) {
        if (Functions == null || Functions.Count == 0 || function.IsNullOrWhiteSpace()) return null;

        var functionRule = Functions?.GetByDbName(function);
        if (functionRule != null) return functionRule;

        functionRule = Functions?.GetByFinalName(function);
        if (functionRule?.DbName.HasNonWhiteSpace() == true) functionRule = null;

        return functionRule;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public EntityRuleNode AddEntity(string tableName) {
        var entityRule = new EntityRuleNode(new EntityRule {
            Name = tableName,
            IncludeUnknownColumns = true
        }, this);
        Entities.Add(entityRule);
        Rule.Entities.Add(entityRule);
        return entityRule;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public FunctionRuleNode AddFunction(string functionName) {
        var functionRuleNode = new FunctionRuleNode(new FunctionRule {
            Name = functionName
        }, this);
        Functions.Add(functionRuleNode);
        Rule.Functions.Add(functionRuleNode);
        return functionRuleNode;
    }

    /// <summary> implicit conversion </summary>
    public static implicit operator SchemaRule(SchemaRuleNode o) => o?.Rule;
}