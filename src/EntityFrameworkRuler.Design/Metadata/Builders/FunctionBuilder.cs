﻿using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

#pragma warning disable CS1591

namespace EntityFrameworkRuler.Design.Metadata.Builders;

public class FunctionBuilder : AnnotatableBuilder<Function, ModelBuilderEx> {
    public FunctionBuilder(Function metadata, ModelBuilderEx modelBuilder) : base(metadata, modelBuilder) { }

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

    public FunctionBuilder HasReturnType(string returnType) {
        Metadata.ReturnType = returnType;
        return this;
    }

    public FunctionBuilder HasCommandText(string commandText) {
        Metadata.CommandText = commandText;
        return this;
    }

    public FunctionBuilder SupportsMultipleResultSet(bool supportsMultipleResultSet) {
        Metadata.SupportsMultipleResultSet = supportsMultipleResultSet;
        return this;
    }
}