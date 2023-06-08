using System.Collections.Immutable;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

#pragma warning disable CS1591

namespace EntityFrameworkRuler.Design.Metadata.Builders;

public class FunctionBuilder : AnnotatableBuilder<Function, ModelBuilderEx> {
    public FunctionBuilder(Function metadata, ModelBuilderEx modelBuilder) : base(metadata, modelBuilder) { }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IMutableModel Model => ModelBuilder.Model;
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ModelEx ModelEx => ModelBuilder.ModelEx;
    
    /// <summary>
    ///     Adds or updates an annotation on the function. If an annotation with the key specified in
    ///     <paramref name="annotation" /> already exists its value will be updated.
    /// </summary>
    /// <param name="annotation">The key of the annotation to be added or updated.</param>
    /// <param name="value">The value to be stored in the annotation.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public virtual FunctionBuilder HasAnnotation(string annotation, object value) {
        if (annotation.IsNullOrEmpty()) throw new ArgumentNullException(nameof(annotation));
        base.HasAnnotation(annotation, value, ConfigurationSource.Explicit);
        return this;
    }

    public ParameterBuilder CreateParameter(string paramName) {
        return Metadata.CreateParameter(paramName);
    }

    public FunctionBuilder HasMappedType(string returnType) {
        Metadata.MappedType = returnType;
        return this;
    }

    public FunctionBuilder HasSchema(string schema) {
        Metadata.Schema = schema;
        return this;
    }

    public FunctionBuilder HasCommandText(string commandText) {
        Metadata.CommandText = commandText;
        return this;
    }
    public FunctionBuilder HasDatabaseName(string databaseName) {
        Metadata.DatabaseName = databaseName;
        return this;
    }

    public FunctionBuilder SupportsMultipleResultSet(bool supportsMultipleResultSet) {
        Metadata.SupportsMultipleResultSet = supportsMultipleResultSet;
        return this;
    }

    public FunctionBuilder HasMultiResultTupleSyntax(string multiResultTupleSyntax) {
        Metadata.MultiResultTupleSyntax = multiResultTupleSyntax;
        return this;
    }

    public FunctionBuilder HasValidResultSet(bool hasValidResultSet) {
        Metadata.HasValidResultSet = hasValidResultSet;
        return this;
    }

    public FunctionBuilder HasResult(IList<DatabaseFunctionResultTable> results) {
        Metadata.Results = results;
        return this;
    }

    public FunctionBuilder HasReturnType(string returnType) {
        Metadata.ReturnType = returnType;
        return this;
    }
    public FunctionBuilder HasScalar(bool isScalar) {
        Metadata.IsScalar = isScalar;
        return this;
    }
    public FunctionBuilder HasFunctionType(FunctionType functionType) {
        Metadata.FunctionType= functionType;
        return this;
    }

    public FunctionBuilder If(Func<bool> condition, Func<FunctionBuilder, FunctionBuilder> then) {
        return condition() ? then(this) : this;
    }

    public void AddResultEntity(IMutableEntityType resultEntity) {
        Metadata.ResultEntities.Add(resultEntity);
    }
}