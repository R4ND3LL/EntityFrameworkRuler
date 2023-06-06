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
    private readonly  ParameterBuilder builder;

    public Parameter(Function function, string name) {
        builder = new(this, function.Model.BuilderEx);
        Function = function;
        Name = name;
    }

    public Function Function { get; }
    public virtual ModelEx Model => Function.Model;
    IModel IReadOnlyParameter.Model => (IModel)Model.Model;
    public virtual ParameterBuilder Builder { [DebuggerStepThrough] get => builder ?? throw new InvalidOperationException(CoreStrings.ObjectRemovedFromModel); }

    /// <summary> Gets the name of this parameter. </summary>
    public virtual string Name { [DebuggerStepThrough] get; }
    public string StoreType { get; set; }
    public SqlDbType SqlDbType { get; set; }
    public bool IsOutput { get; set; }
    public Type ClrType { get; set; }
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public int? Order { get; set; }
}