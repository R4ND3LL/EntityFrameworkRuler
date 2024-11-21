using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Design.Metadata.Builders;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

#pragma warning disable CS1591

namespace EntityFrameworkRuler.Design.Metadata;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class Function : ConventionAnnotatable, IFunction {
    private readonly FunctionBuilder builder;
    private readonly SortedDictionary<string, Parameter> parameters = new(StringComparer.Ordinal);

    public Function(ModelEx model, string name) {
        Model = model;
        builder = new(this, model.BuilderEx);
        Name = name;
    }

    public virtual ModelEx Model { [DebuggerStepThrough] get; }
    IModel IReadOnlyFunction.Model => (IModel)Model.Model;


    public virtual FunctionBuilder Builder {
        [DebuggerStepThrough] get => builder ??
#if NET9_0_OR_GREATER
               throw new InvalidOperationException(CoreStrings.ObjectRemovedFromModel(Name));
#else
               throw new InvalidOperationException(CoreStrings.ObjectRemovedFromModel);
#endif
    }

    /// <summary> Gets the name of this function. </summary>
    public virtual string Name { [DebuggerStepThrough] get; }

    /// <summary> Gets the database name of this function. </summary>
    public virtual string DatabaseName { [DebuggerStepThrough] get; set; }

    /// <summary> Gets the SQL command text to execute this function. </summary>
    public virtual string CommandText { [DebuggerStepThrough] get; set; }

    public virtual string Schema { [DebuggerStepThrough] get; set; }
    public virtual bool SupportsMultipleResultSet { [DebuggerStepThrough] get; set; }
    public virtual string MultiResultTupleSyntax { [DebuggerStepThrough] get; set; }
    public virtual bool HasAcquiredResultSchema { [DebuggerStepThrough] get; set; }
    public virtual string ReturnType { [DebuggerStepThrough] get; set; }

    /// <summary> Query always returns only one value as opposed to a list or complex return type </summary>
    public virtual bool IsScalar { [DebuggerStepThrough] get; set; }

    public virtual bool IsTableValuedFunction => FunctionType == FunctionType.Function && !IsScalar;
    public virtual FunctionType FunctionType { [DebuggerStepThrough] get; set; }
    public IList<IMutableEntityType> ResultEntities { [DebuggerStepThrough] get; } = new List<IMutableEntityType>();

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