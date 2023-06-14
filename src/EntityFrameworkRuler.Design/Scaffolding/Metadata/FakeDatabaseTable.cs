using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Scaffolding.Metadata;

#pragma warning disable CS1591
/// <summary>
/// Represents a database table added at scaffolding runtime in order to generate an entity, however, the entity
/// will not be based on a real table or view.  for example, entities used to represent stored procedure results.
/// </summary>
public abstract class FakeDatabaseTable : DatabaseTable {
    public abstract bool ShouldScaffoldEntityFromTable { get; }
}