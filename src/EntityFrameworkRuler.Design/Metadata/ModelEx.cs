using EntityFrameworkRuler.Common.Annotations;
using EntityFrameworkRuler.Design.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Metadata;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public class ModelEx {
    private readonly SortedDictionary<string, Function> functions = new(StringComparer.Ordinal);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ModelEx(ModelBuilderEx builder) {
        BuilderEx = builder;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IMutableModel Model => Builder.Model;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ModelBuilderEx BuilderEx { get; }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public ModelBuilder Builder => BuilderEx.Builder;

    /// <summary> Get all functions defined in the model. </summary>
    public IEnumerable<Function> GetFunctions() => functions.Values;

    /// <summary>
    ///     Gets all entity types defined in the model.
    /// </summary>
    /// <returns>All entity types defined in the model.</returns>
    public IEnumerable<IMutableEntityType> GetEntityTypes() => Model.GetEntityTypes();

    /// <summary>
    ///     Gets the entity types matching the given type.
    /// </summary>
    /// <param name="type">The type of the entity type to find.</param>
    /// <returns>The entity types found.</returns>
    public IEnumerable<IMutableEntityType> FindEntityTypes(Type type) => Model.FindEntityTypes(type);

    /// <summary>
    ///     Gets the entity with the given name. Returns <see langword="null" /> if no entity type with the given name is found
    ///     or the given CLR type is being used by shared type entity type
    ///     or the entity type has a defining navigation.
    /// </summary>
    /// <param name="name">The name of the entity type to find.</param>
    /// <returns>The entity type, or <see langword="null" /> if none is found.</returns>
    public IMutableEntityType FindEntityType(string name) => Model.FindEntityType(name);

    /// <summary>
    ///     Gets the entity type for the given name, defining navigation name
    ///     and the defining entity type. Returns <see langword="null" /> if no matching entity type is found.
    /// </summary>
    /// <param name="name">The name of the entity type to find.</param>
    /// <param name="definingNavigationName">The defining navigation of the entity type to find.</param>
    /// <param name="definingEntityType">The defining entity type of the entity type to find.</param>
    /// <returns>The entity type, or <see langword="null" /> if none is found.</returns>
    public IMutableEntityType FindEntityType(
        string name,
        string definingNavigationName,
        IMutableEntityType definingEntityType) => Model.FindEntityType(name, definingNavigationName, definingEntityType);

    /// <summary>
    ///     Gets the entity that maps the given entity class. Returns <see langword="null" /> if no entity type with
    ///     the given CLR type is found or the given CLR type is being used by shared type entity type
    ///     or the entity type has a defining navigation.
    /// </summary>
    /// <param name="type">The type to find the corresponding entity type for.</param>
    /// <returns>The entity type, or <see langword="null" /> if none is found.</returns>
    public IMutableEntityType FindEntityType(Type type) => Model.FindEntityType(type);

    /// <summary>
    ///     Gets the entity type for the given name, defining navigation name
    ///     and the defining entity type. Returns <see langword="null" /> if no matching entity type is found.
    /// </summary>
    /// <param name="type">The type of the entity type to find.</param>
    /// <param name="definingNavigationName">The defining navigation of the entity type to find.</param>
    /// <param name="definingEntityType">The defining entity type of the entity type to find.</param>
    /// <returns>The entity type, or <see langword="null" /> if none is found.</returns>
    public IMutableEntityType FindEntityType(
        Type type,
        string definingNavigationName,
        IMutableEntityType definingEntityType) => Model.FindEntityType(type, definingNavigationName, definingEntityType);

    /// <summary>
    ///     Returns a value indicating whether the entity types using the given type should be configured
    ///     as owned types when discovered by conventions.
    /// </summary>
    /// <param name="type">The type of the entity type that might be owned.</param>
    /// <returns>
    ///     <see langword="true" /> if a matching entity type should be configured as owned when discovered,
    ///     <see langword="false" /> otherwise.
    /// </returns>
    public bool IsOwned(Type type) => Model.IsOwned(type);

    /// <summary>
    ///     Indicates whether the given entity type name is ignored.
    /// </summary>
    /// <param name="typeName">The name of the entity type that might be ignored.</param>
    /// <returns><see langword="true" /> if the given entity type name is ignored.</returns>
    public bool IsIgnored(string typeName) => Model.IsIgnored(typeName);

    /// <summary>
    ///     Indicates whether the given entity type name is ignored.
    /// </summary>
    /// <param name="type">The entity type that might be ignored.</param>
    /// <returns><see langword="true" /> if the given entity type name is ignored.</returns>
    public bool IsIgnored(Type type) => Model.IsIgnored(type);

    /// <summary>
    ///     Returns an object that can be used to configure a given entity type in the model.
    ///     If an entity type with the provided name is not already part of the model,
    ///     a new entity type that does not have a corresponding CLR type will be added to the model.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types</see> for more information and examples.
    /// </remarks>
    /// <param name="name">The name of the entity type to be configured.</param>
    /// <returns>An object that can be used to configure the entity type.</returns>
    public virtual FunctionBuilder CreateFunction(string name) {
        if (functions.ContainsKey(name)) throw new Exception($"Function {name} already exists");
        var function = new Function(this, name);
        functions.Add(name, function);
        return function.Builder;
    }
}