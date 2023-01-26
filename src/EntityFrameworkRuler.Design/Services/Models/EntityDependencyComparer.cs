namespace EntityFrameworkRuler.Design.Services.Models;

/// <summary> Entity rule comparer that sorts by base type dependency then name. </summary>
public sealed class EntityDependencyComparer : IComparer<EntityRuleNode>, IEqualityComparer<EntityRuleNode> {
    private EntityDependencyComparer() { }

    /// <summary> The singleton instance of the comparer to use. </summary>
    public static readonly EntityDependencyComparer Instance = new();

    /// <summary> Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other. </summary>
    public int Compare(EntityRuleNode x, EntityRuleNode y) {
        if (ReferenceEquals(x, y)) return 0;

        if (x == null) return -1;
        if (y == null) return 1;

        if (!ReferenceEquals(x.BaseEntityRuleNode, y.BaseEntityRuleNode)) {
            if (x.BaseEntityRuleNode == null) return -1;
            if (y.BaseEntityRuleNode == null) return 1;

            if (x.HasBaseType(y)) return 1;
            if (y.HasBaseType(x)) return -1;
        }

        var schemaCompare = StringComparer.Ordinal.Compare(x.Parent.Rule.SchemaName, y.Parent.Rule.SchemaName);
        if (schemaCompare != 0) return schemaCompare;

        var nameCompare = StringComparer.Ordinal.Compare(x.DbName.CoalesceWhiteSpace(x.GetFinalName()),
            y.DbName.CoalesceWhiteSpace(y.GetFinalName()));
        if (nameCompare != 0) return nameCompare;

        // at this point, there must be 2 tables based on the same entity. ensure larger table is first
        // in case of split tables, we want the largest to be default if possible
        var c = x.LocalPropertyCount.CompareTo(y.LocalPropertyCount);
        if (c != 0) return -c;
        c = x.GetHashCode().CompareTo(y.GetHashCode());
        if (c != 0) return c;
        return 1;
    }

    /// <summary> Determines whether the specified objects are equal. </summary>
    public bool Equals(EntityRuleNode x, EntityRuleNode y) => Compare(x, y) == 0;

    /// <summary> Returns a hash code for the specified object. </summary>
    public int GetHashCode(EntityRuleNode obj) => obj.GetFinalName().GetHashCode();
}

/// <summary> Entity rule comparer that sorts by base type property size descending. </summary>
public sealed class EntitySizeComparer : IComparer<EntityRuleNode>, IEqualityComparer<EntityRuleNode> {
    private EntitySizeComparer() { }

    /// <summary> The singleton instance of the comparer to use. </summary>
    public static readonly EntitySizeComparer Instance = new();

    /// <summary> Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other. </summary>
    public int Compare(EntityRuleNode x, EntityRuleNode y) {
        if (ReferenceEquals(x, y)) return 0;

        if (x == null) return -1;
        if (y == null) return 1;

        // at this point, there must be 2 tables based on the same entity. ensure larger table is first
        // in case of split tables, we want the largest to be default if possible
        var c = x.LocalPropertyCount.CompareTo(y.LocalPropertyCount);
        if (c != 0) return -c;
        c = x.GetHashCode().CompareTo(y.GetHashCode());
        if (c != 0) return c;
        return 1;
    }

    /// <summary> Determines whether the specified objects are equal. </summary>
    public bool Equals(EntityRuleNode x, EntityRuleNode y) => Compare(x, y) == 0;

    /// <summary> Returns a hash code for the specified object. </summary>
    public int GetHashCode(EntityRuleNode obj) => obj.GetFinalName().GetHashCode();
}