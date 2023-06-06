// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Metadata;

/// <summary> Represents a Function in a model. </summary>
public interface IFunction : IReadOnlyFunction { }

/// <summary> Represents a Function in a model. </summary>
public interface IReadOnlyFunction : IReadOnlyAnnotatable {
    /// <summary> Gets the model that this type belongs to. </summary>
    new IModel Model { get; }

    /// <summary> Gets the name of this dbFunction. </summary>
    string Name { [DebuggerStepThrough] get; }
}

