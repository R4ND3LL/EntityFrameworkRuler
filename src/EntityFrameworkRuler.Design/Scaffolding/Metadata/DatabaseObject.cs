using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkRuler.Design.Scaffolding.Metadata;

#pragma warning disable CS1591
public abstract class DatabaseObject : Annotatable {
    public string Name { get; set; }
    public string Schema { get; set; }

    public override string ToString() => $"{Schema}.{Name}";
}