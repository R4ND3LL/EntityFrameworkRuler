using EntityFrameworkRuler.Design.Services.Models;

#pragma warning disable CS1591
namespace EntityFrameworkRuler.Design.Metadata;

/// <summary>
/// Base object for functions and stored procedures.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "DTO")]
public abstract class Routine : SqlObjectBase {
    public bool HasValidResultSet { get; set; }
    public int UnnamedColumnCount { get; set; }
    public abstract bool IsScalar { get; set; }
    public bool SupportsMultipleResultSet => Results.Count > 1;
    public bool NoResultSet => Results.Count == 1 && Results[0].Count == 0 && HasValidResultSet;

    public string MappedType { get; set; }
    public List<ModuleParameter> Parameters { get; set; } = new List<ModuleParameter>();
    public List<List<ModuleResultElement>> Results { get; set; } = new List<List<ModuleResultElement>>();
}

public sealed class Procedure : Routine {
    public override bool IsScalar { get => false; set { } }
}

public sealed class Function : Routine {
    public override bool IsScalar { get; set; }
}
public sealed class ModuleParameter {
    public string Name { get; set; }
    public string StoreType { get; set; }
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool Output { get; set; }
    public bool Nullable { get; set; }
    public string TypeName { get; set; }
    public int? TypeId { get; set; }
    public int? TypeSchema { get; set; }
    public bool IsReturnValue { get; set; }
    public string RoutineName { get; set; }
    public string RoutineSchema { get; set; }
}

public sealed class ModuleResultElement {
    public string Name { get; set; }
    public string StoreType { get; set; }
    public int Ordinal { get; set; }
    public bool Nullable { get; set; }
    public short? Precision { get; set; }
    public short? Scale { get; set; }
}