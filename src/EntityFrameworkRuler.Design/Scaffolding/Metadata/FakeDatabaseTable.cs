using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Scaffolding.Metadata;

#pragma warning disable CS1591
public abstract class FakeDatabaseTable : DatabaseTable {
    public abstract bool ShouldScaffoldEntityFromTable { get; }
}