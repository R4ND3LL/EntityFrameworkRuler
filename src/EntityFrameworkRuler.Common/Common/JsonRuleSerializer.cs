using System.Diagnostics.CodeAnalysis;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Common;

/// <inheritdoc />
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class JsonRuleSerializer : IRuleSerializer {
    /// <inheritdoc />
    public Task Serialize<T>(T model, string path) where T : class, IRuleModelRoot {
        return model.WriteJsonFile<T>(path);
    }

    /// <inheritdoc />
    public Task<T> Deserialize<T>(string filePath) where T : class, new() {
        return filePath.ReadJsonFile<T>();
    }
}