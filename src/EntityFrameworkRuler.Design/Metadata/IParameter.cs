using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Metadata;

/// <summary> Represents a Parameter in a model. </summary>
public interface IParameter : IReadOnlyParameter { }

/// <summary> Represents a Parameter in a model. </summary>
public interface IReadOnlyParameter : IReadOnlyAnnotatable {
    /// <summary> Gets the model that this type belongs to. </summary>
    new IModel Model { get; }

    /// <summary> Gets the name of this dbParameter. </summary>
    string Name { [DebuggerStepThrough] get; }
}