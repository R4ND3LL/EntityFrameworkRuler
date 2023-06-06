using EntityFrameworkRuler.Common.Annotations;
using EntityFrameworkRuler.Design.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRuler.Design.Metadata;

public class ModelEx {
    private SortedDictionary<string, Function> functions = new(StringComparer.Ordinal);

    public ModelEx(ModelBuilderEx builder) {
        BuilderEx = builder;
        //builder.Builder.HasAnnotation(RulerAnnotations.ModelExtension, this);
    }

    public IMutableModel Model => Builder.Model;
    public ModelBuilderEx BuilderEx { get; }
    public ModelBuilder Builder => BuilderEx.Builder;

    public IEnumerable<Function> GetFunctions() => functions.Values;

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