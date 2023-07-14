using EntityFrameworkRuler.Common.Annotations;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Scaffolding.Metadata;

#pragma warning disable CS1591
/// <summary>
/// Represents a database table added at scaffolding runtime in order to generate an entity, however, the entity
/// will not be based on a real table or view.  for example, entities used to represent stored procedure results.
/// </summary>
public abstract class FakeDatabaseTable : DatabaseTable {
    
    public virtual int Ordinal {
        get => (int?)base.GetAnnotation(RulerAnnotations.Ordinal).Value ?? 0;
        set => base.SetAnnotation(RulerAnnotations.Ordinal, value);
    }
    public abstract bool ShouldScaffoldEntityFromTable { get; }
}

public abstract class FakeDatabaseColumn : DatabaseColumn {
    public virtual bool HasName => Name.HasNonWhiteSpace();

    public virtual int Ordinal {
        get => (int?)base.GetAnnotation(RulerAnnotations.Ordinal).Value ?? 0;
        set => base.SetAnnotation(RulerAnnotations.Ordinal, value);
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