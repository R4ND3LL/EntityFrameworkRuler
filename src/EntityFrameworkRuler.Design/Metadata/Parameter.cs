using System.Data;
using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Metadata;

#pragma warning disable CS1591
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class Parameter : ConventionAnnotatable, IParameter {
    private readonly ParameterBuilder builder;

    public Parameter(Function function, string name) {
        builder = new(this, function.Model.BuilderEx);
        Function = function;
        Name = name;
    }

    public Function Function { [DebuggerStepThrough] get; }
    public virtual ModelEx Model => Function.Model;
    IModel IReadOnlyParameter.Model => (IModel)Model.Model;
    public virtual ParameterBuilder Builder { [DebuggerStepThrough] get => builder ?? throw new InvalidOperationException(CoreStrings.ObjectRemovedFromModel); }

    /// <summary> Gets the name of this parameter. </summary>
    public virtual string Name { [DebuggerStepThrough] get; }
    public virtual string StoreType { [DebuggerStepThrough] get; set; }
    public virtual SqlDbType SqlDbType { [DebuggerStepThrough] get; set; }
    public virtual string TypeName { [DebuggerStepThrough] get; set; }
    public virtual bool IsOutput { [DebuggerStepThrough] get; set; }

    public virtual bool IsNullable { [DebuggerStepThrough] get; set; }
    public virtual bool IsReturnValue { [DebuggerStepThrough] get; set; }
    public virtual Type ClrType { [DebuggerStepThrough] get; set; }
    public virtual int? Length { [DebuggerStepThrough] get; set; }
    public virtual int? Precision { [DebuggerStepThrough] get; set; }
    public virtual int? Scale { [DebuggerStepThrough] get; set; }
    public virtual int? Order { [DebuggerStepThrough] get; set; }
}