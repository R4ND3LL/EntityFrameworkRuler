using EntityFrameworkRuler.Common.Annotations;
using EntityFrameworkRuler.Design.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

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
    public virtual IList<DatabaseFunctionResultTable> Results { get; } = new List<DatabaseFunctionResultTable>();
}

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

public class DatabaseFunctionResultTable : DatabaseTable {
    public DatabaseFunctionResultTable() { }

    public new IEnumerable<DatabaseFunctionResultColumn> ResultColumns =>
        base.Columns.Cast<DatabaseFunctionResultColumn>(); // { get; set; } = new List<DatabaseFunctionResultColumn>();

    public int Count => Columns?.Count ?? 0;
    public DatabaseFunctionResultColumn this[int i] { get => (DatabaseFunctionResultColumn)Columns[i]; set => Columns[i] = value; }

    public virtual int Ordinal {
        get => (int?)base.GetAnnotation(RulerAnnotations.Ordinal).Value ?? 0;
        set => base.SetAnnotation(RulerAnnotations.Ordinal, value);
    }

    public DatabaseFunction Function { get; set; }
    // {
    //     get => (DatabaseFunction?)base.GetAnnotation(RulerAnnotations.Function).Value  ;
    //     set => base.SetAnnotation(RulerAnnotations.Ordinal, value);
    // }
}

public class DatabaseFunctionResultColumn : DatabaseColumn {
    public DatabaseFunctionResultColumn() { }

    // public virtual string Name { get; set; }
    // public virtual string StoreType { get; set; }
    public virtual int Ordinal {
        get => (int?)base.GetAnnotation(RulerAnnotations.Ordinal).Value ?? 0;
        set => base.SetAnnotation(RulerAnnotations.Ordinal, value);
    }

    public virtual bool Nullable {
        get => (bool?)base.GetAnnotation(RulerAnnotations.Nullable).Value ?? false;
        set => base.SetAnnotation(RulerAnnotations.Nullable, value);
    }

    public virtual int? Precision {
        get => (int?)base.GetAnnotation(EfCoreAnnotationNames.Precision).Value;
        set => base.SetAnnotation(EfCoreAnnotationNames.Precision, value);
    }

    public virtual int? Scale {
        get => (int?)base.GetAnnotation(EfCoreAnnotationNames.Scale).Value;
        set => base.SetAnnotation(EfCoreAnnotationNames.Scale, value);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name ?? "<UNKNOWN>"}: {StoreType}";
}