using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Common;

/// <inheritdoc />
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class JsonRuleSerializer : IRuleSerializer {
    /// <inheritdoc />
    public Task Serialize<T>(T model, string path) where T : class, IRuleModelRoot {
        return model.ToJson<T>(path);
    }

    /// <inheritdoc />
    public Task<T> TryDeserializeFile<T>(string filePath) where T : class, new() {
        return filePath.TryReadJsonFile<T>();
    }
}