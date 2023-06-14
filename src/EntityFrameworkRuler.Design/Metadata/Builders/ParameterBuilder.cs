using System.Data;
using EntityFrameworkRuler.Design.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable CS1591

namespace EntityFrameworkRuler.Design.Metadata.Builders;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public class ParameterBuilder : AnnotatableBuilder<Parameter, ModelBuilderEx> {
    public ParameterBuilder(Parameter metadata, ModelBuilderEx modelBuilder) : base(metadata, modelBuilder) { }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IMutableModel Model => ModelBuilder.Model;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ModelEx ModelEx => ModelBuilder.ModelEx;

    /// <summary>
    ///     Adds or updates an annotation on the parameter. If an annotation with the key specified in
    ///     <paramref name="annotation" /> already exists its value will be updated.
    /// </summary>
    /// <param name="annotation">The key of the annotation to be added or updated.</param>
    /// <param name="value">The value to be stored in the annotation.</param>
    /// <returns>The same builder instance so that multiple configuration calls can be chained.</returns>
    public virtual ParameterBuilder HasAnnotation(string annotation, object value) {
        if (annotation.IsNullOrEmpty()) throw new ArgumentNullException(nameof(annotation));
        base.HasAnnotation(annotation, value, ConfigurationSource.Explicit);
        return this;
    }

    public ParameterBuilder HasStoreType(string dbParameterStoreType) {
        Metadata.StoreType = dbParameterStoreType;
        return this;
    }

    public ParameterBuilder HasOutput(bool isOutput = true) {
        Metadata.IsOutput = isOutput;
        return this;
    }

    public ParameterBuilder HasNullable(bool isNullable = true) {
        Metadata.IsNullable = isNullable;
        return this;
    }

    public ParameterBuilder HasLength(int? length) {
        Metadata.Length = length;
        return this;
    }

    public ParameterBuilder HasScale(int? scale) {
        Metadata.Scale = scale;
        return this;
    }

    public ParameterBuilder HasPrecision(int? precision) {
        Metadata.Precision = precision;
        return this;
    }

    public ParameterBuilder HasSqlDbType(SqlDbType sqlDbType) {
        Metadata.SqlDbType = sqlDbType;
        return this;
    }

    public ParameterBuilder HasReturnValue(bool isReturnValue = true) {
        Metadata.IsReturnValue = isReturnValue;
        return this;
    }

    public ParameterBuilder HasTypeName(string typeName) {
        Metadata.TypeName = typeName;
        return this;
    }

    public ParameterBuilder HasClrType(Type clrType) {
        Metadata.ClrType = clrType;
        return this;
    }

    public ParameterBuilder If(Func<bool> condition, Func<ParameterBuilder, ParameterBuilder> then) {
        return condition() ? then(this) : this;
    }
}