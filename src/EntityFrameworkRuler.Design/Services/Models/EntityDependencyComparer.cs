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

        var n = StringComparer.Ordinal.Compare(x.GetFinalName() ?? x.DbName, y.GetFinalName() ?? y.DbName);
        if (n != 0) return n;
        n = x.GetHashCode().CompareTo(y.GetHashCode());
        if (n != 0) return n;
        return 1;
    }

    /// <summary> Determines whether the specified objects are equal. </summary>
    public bool Equals(EntityRuleNode x, EntityRuleNode y) => Compare(x, y) == 0;

    /// <summary> Returns a hash code for the specified object. </summary>
    public int GetHashCode(EntityRuleNode obj) => obj.GetFinalName().GetHashCode();
}