using EntityFrameworkRuler.Common.Annotations;
using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Services.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

#pragma warning disable CS1591
namespace EntityFrameworkRuler.Design.Scaffolding.Metadata;

/// <summary> Base object for functions and stored procedures. </summary>
public class DatabaseFunction : DatabaseObject {
    public virtual bool HasAcquiredResultSchema { get; set; }
    public virtual int UnnamedColumnCount { get; set; }
    public virtual bool IsScalar { get; set; }
    public virtual bool IsTableValuedFunction => FunctionType == FunctionType.Function && !IsScalar;
    public virtual bool SupportsMultipleResultSet => Results.Count > 1;
    public virtual bool NoResultSet => Results != null && Results.Count == 1 && Results[0].Count == 0 && HasAcquiredResultSchema;
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
    public virtual int Ordinal { get; set; }
    public virtual bool IsOutput { get; set; }
    public virtual bool IsNullable { get; set; }
    public virtual string TypeName { get; set; }
    public virtual int? TypeId { get; set; }
    public virtual int? TypeSchema { get; set; }
    public virtual bool IsReturnValue { get; set; }
    public virtual string FunctionName { get; set; }
    public virtual string FunctionSchema { get; set; }
    public virtual string Collation { get; set; }

    /// <summary> The default constraint for the column, or <see langword="null" /> if none. </summary>
    public virtual string DefaultValueSql { get; set; }

    public override string ToString() => $"{StoreType} {Name ?? "<UNKNOWN>"}";
}

public class DatabaseFunctionResultTable : FakeDatabaseTable {
    public DatabaseFunctionResultTable() { }

    public IEnumerable<DatabaseFunctionResultColumn> ResultColumns =>
        base.Columns.Cast<DatabaseFunctionResultColumn>();

    public int Count => Columns?.Count ?? 0;
    public DatabaseFunctionResultColumn this[int i] { get => (DatabaseFunctionResultColumn)Columns[i]; set => Columns[i] = value; }

    public DatabaseFunction Function { get; set; }
    public override bool ShouldScaffoldEntityFromTable => Count > 0 && Function?.UnnamedColumnCount <= 1;
}

public class DatabaseFunctionResultColumn : FakeDatabaseColumn {
    public DatabaseFunctionResultColumn() { }

    public virtual bool Nullable {
        get => (bool?)base.GetAnnotation(RulerAnnotations.Nullable).Value ?? false;
        set => base.SetAnnotation(RulerAnnotations.Nullable, value);
    }

}

public class TphDatabaseTable : FakeDatabaseTable {
    public TphDatabaseTable() { }

    public IEnumerable<TphDatabaseColumn> TphColumns =>
        base.Columns.Cast<TphDatabaseColumn>();

    public int Count => Columns?.Count ?? 0;
    public TphDatabaseColumn this[int i] { get => (TphDatabaseColumn)Columns[i]; set => Columns[i] = value; }

  

    public EntityRuleNode EntityRuleNode { get; set; }
    public override bool ShouldScaffoldEntityFromTable => EntityRuleNode != null;
}

public class TphDatabaseColumn : FakeDatabaseColumn {
    public TphDatabaseColumn() { }

    public PropertyRuleNode PropertyRuleNode { get; set; }
}