using System;
using System.Collections.Generic;
using System.Linq;
using EntityFrameworkRuler.Design.Services.Models;
using EntityFrameworkRuler.Rules;
using Xunit;

namespace EntityFrameworkRuler.Design.Tests;

public class EntityDependencyComparerTests {
    [Fact]
    public void Mixed_hierarchies_keep_one_base_first_order_for_every_declaration_order() {
        var definitions = new[] { ("PartDesign", "PartDesign", (string)null), ("Core", "Core", "Tool"),
            ("Lens", "LensDesign", "PartDesign"), ("Tool", "Tool", "PartDesign") };
        string[] expected = { "PartDesign", "LensDesign", "Tool", "Core" };
        foreach (var order in Permutations(Enumerable.Range(0, definitions.Length).ToArray())) {
            var rules = order.Select(i => new EntityRule { Name = definitions[i].Item1, NewName = definitions[i].Item2,
                BaseTypeName = definitions[i].Item3 });
            var nodes = Nodes(rules);
            Assert.Equal(expected, nodes.Select(n => n.GetFinalName()).ToArray());
            foreach (var node in nodes)
                foreach (var parent in node.GetBaseTypes()) Assert.True(Array.IndexOf(nodes, parent) < Array.IndexOf(nodes, node));
            var comparer = EntityDependencyComparer.Instance;
            foreach (var x in nodes) foreach (var y in nodes) foreach (var z in nodes)
                if (comparer.Compare(x, y) < 0 && comparer.Compare(y, z) < 0) Assert.True(comparer.Compare(x, z) < 0);
        }
    }

    [Fact]
    public void Equal_size_same_table_entities_have_a_deterministic_named_order() {
        string[] names = { "Gamma", "Alpha", "Beta" };
        foreach (var order in Permutations(Enumerable.Range(0, names.Length).ToArray())) {
            var rules = order.Select(i => new EntityRule { Name = "SameTable", NewName = names[i],
                Properties = new List<PropertyRule> { new() { Name = "ID" } } });
            Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, Nodes(rules).Select(n => n.GetFinalName()).ToArray());
        }
    }

    [Fact]
    public void Larger_split_entity_still_precedes_smaller_named_peer() {
        var large = new EntityRule { Name = "SameTable", NewName = "Zeta", Properties = new List<PropertyRule> {
            new() { Name = "ID" }, new() { Name = "Value" }, new() { Name = "Detail" } } };
        var small = new EntityRule { Name = "SameTable", NewName = "Alpha", Properties = new List<PropertyRule> { new() { Name = "ID" } } };
        Assert.Equal(new[] { "Zeta", "Alpha" }, Nodes(new[] { small, large }).Select(n => n.GetFinalName()).ToArray());
    }

    private static EntityRuleNode[] Nodes(IEnumerable<EntityRule> entities) {
        var schema = new SchemaRule { SchemaName = "dbo", Entities = entities.ToList() };
        var rule = new DbContextRule { Name = "OrderProof", Schemas = new List<SchemaRule> { schema } };
        return new DbContextRuleNode(rule).Entities.ToArray();
    }

    private static IEnumerable<int[]> Permutations(int[] values) {
        if (values.Length == 0) { yield return Array.Empty<int>(); yield break; }
        foreach (var value in values)
            foreach (var rest in Permutations(values.Where(i => i != value).ToArray()))
                yield return new[] { value }.Concat(rest).ToArray();
    }
}