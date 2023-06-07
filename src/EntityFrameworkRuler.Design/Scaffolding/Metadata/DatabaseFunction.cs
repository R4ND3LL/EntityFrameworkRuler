using EntityFrameworkRuler.Design.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;

#pragma warning disable CS1591
namespace EntityFrameworkRuler.Design.Scaffolding.Metadata;

/// <summary> Base object for functions and stored procedures. </summary>
public class DatabaseFunction : DatabaseObject {
    public virtual bool HasValidResultSet { get; set; }
    public virtual int UnnamedColumnCount { get; set; }
    public virtual bool IsScalar { get; set; }
    public virtual bool SupportsMultipleResultSet => Results.Count > 1;
    public virtual bool NoResultSet => Results != null && Results.Count == 1 && Results[0].Count == 0 && HasValidResultSet;
    public virtual string MappedType { get; set; }
    public virtual FunctionType FunctionType { get; set; }
    public virtual IList<DatabaseFunctionParameter> Parameters { get; set; } = new List<DatabaseFunctionParameter>();
    public virtual IList<List<DatabaseFunctionResultElement>> Results { get; } = new List<List<DatabaseFunctionResultElement>>();
}

// public class DatabaseProcedure : DatabaseFunction { }
//
// public class DatabaseFunction : DatabaseProcedure { }

public class DatabaseFunctionParameter : Annotatable {
    public virtual string Name { get; set; }
    public virtual string StoreType { get; set; }
    public virtual int? Length { get; set; }
    public virtual int? Precision { get; set; }
    public virtual int? Scale { get; set; }
    public virtual bool IsOutput { get; set; }
    public virtual bool IsNullable { get; set; }
    public virtual string TypeName { get; set; }
    public virtual int? TypeId { get; set; }
    public virtual int? TypeSchema { get; set; }
    public virtual bool IsReturnValue { get; set; }
    public virtual string FunctionName { get; set; }
    public virtual string FunctionSchema { get; set; }

    /// <summary> The default constraint for the column, or <see langword="null" /> if none. </summary>
    public virtual string DefaultValueSql { get; set; }

    public override string ToString() => $"{StoreType} {Name ?? "<UNKNOWN>"}";
}

public class DatabaseFunctionResultElement : Annotatable {
    public virtual string Name { get; set; }
    public virtual string StoreType { get; set; }
    public virtual int Ordinal { get; set; }
    public virtual bool Nullable { get; set; }
    public virtual short? Precision { get; set; }
    public virtual short? Scale { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{Name ?? "<UNKNOWN>"}: {StoreType}";
}