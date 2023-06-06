using System.Reflection;
using EntityFrameworkRuler.Common.Annotations;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Scaffolding.Metadata;

/// <inheritdoc />
public sealed class DatabaseModelEx : DatabaseModel {
    /// <inheritdoc />
    public DatabaseModelEx(DatabaseModel model) {
        ConsumeModel(model);
        AddAnnotation(RulerAnnotations.Functions, Functions);
    }

    private void ConsumeModel(DatabaseModel model) {
        var props = model.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var prop in props) {
            var value = prop.GetValue(model);
            prop.SetValue(this, value);
        }

        Debug.Assert(Tables.Count == model.Tables.Count);
        Debug.Assert(Sequences.Count == model.Sequences.Count);
        Debug.Assert(Collation == model.Collation);
        Debug.Assert(DatabaseName == model.DatabaseName);
        Debug.Assert(DefaultSchema == model.DefaultSchema);
        foreach (var annotation in model.GetAnnotations()) this.SetAnnotation(annotation.Name, annotation);

        foreach (var annotation in model.GetRuntimeAnnotations()) this.SetRuntimeAnnotation(annotation.Name, annotation);
    }

    /// <summary> Database functions (procedures and functions) </summary>
    public IList<DatabaseFunction> Functions { get; } = new List<DatabaseFunction>();
}