using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Metadata.Builders;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

#pragma warning disable CS1591

namespace EntityFrameworkRuler.Design.Metadata;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class Function : ConventionAnnotatable, IFunction {
    private readonly FunctionBuilder builder;
    private SortedDictionary<string, Parameter> parameters = new(StringComparer.Ordinal);

    public Function(ModelEx model, string name) {
        Model = model;
        builder = new(this, model.BuilderEx);
        Name = name;
    }

    public virtual ModelEx Model { [DebuggerStepThrough] get; }
    IModel IReadOnlyFunction.Model => (IModel)Model.Model;


    public virtual FunctionBuilder Builder {
        [DebuggerStepThrough] get => builder ?? throw new InvalidOperationException(CoreStrings.ObjectRemovedFromModel);
    }

    /// <summary> Gets the name of this type. </summary>
    public virtual string Name { [DebuggerStepThrough] get; }

    /// <summary> Gets the SQL command text to execute this function. </summary>
    public virtual string CommandText { get; set; }

    public virtual string MappedType { get; set; }
    public virtual string Schema { get; set; }
    public virtual bool SupportsMultipleResultSet { get; set; }
    public virtual string MultiResultTupleSyntax { get; set; }
    public virtual bool HasValidResultSet { get; set; }
    public virtual string ReturnType { get; set; }

    /// <summary> Query always returns only one value as opposed to a complex return type </summary>
    public virtual bool IsScalar { get; set; }

    public virtual FunctionType FunctionType { get; set; }
    public IList<DatabaseFunctionResultTable> Results { get; set; }
    public virtual bool NoResultSet => Results != null && Results.Count == 1 && Results[0].Count == 0 && HasValidResultSet;
    public IList<IMutableEntityType> ResultEntities { get; } = new List<IMutableEntityType>();

    public ParameterBuilder CreateParameter(string name) {
        if (parameters.ContainsKey(name)) throw new Exception($"Parameter {name} already exists");
        var parameter = new Parameter(this, name);
        parameters.Add(name, parameter);
        return parameter.Builder;
    }

    public IEnumerable<Parameter> GetParameters() { return parameters.Values; }
}

public enum FunctionType {
    StoredProcedure,
    Function
}