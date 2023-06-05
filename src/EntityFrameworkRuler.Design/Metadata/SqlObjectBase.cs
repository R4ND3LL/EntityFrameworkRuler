namespace EntityFrameworkRuler.Design.Metadata;

#pragma warning disable CS1591
public abstract class SqlObjectBase {
    public string Name { get; set; }
    public string Schema { get; set; }

    public override string ToString() => $"{Schema}.{Name}";
}