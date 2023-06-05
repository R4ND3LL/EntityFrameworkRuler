using System.Reflection;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Metadata;

/// <inheritdoc />
public sealed class DatabaseModelEx : DatabaseModel {
    /// <inheritdoc />
    public DatabaseModelEx(DatabaseModel model) {
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
        foreach (var annotation in model.GetAnnotations()) this.AddAnnotation(annotation.Name, annotation);

        foreach (var annotation in model.GetRuntimeAnnotations()) this.AddRuntimeAnnotation(annotation.Name, annotation);
    }

    public List<Routine> Routines { get; } = new();
}