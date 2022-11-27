using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Common;

/// <summary> Serializer for rule models </summary>
public interface IRuleSerializer {
    /// <summary> Serialize the given model to the supplied file path </summary>
    Task Serialize<T>(T model, string path) where T : class, IRuleModelRoot;

    /// <summary> Read the json file or return NULL on failure </summary>
    /// <param name="filePath">json file path to load</param>
    /// <typeparam name="T">type to deserialize</typeparam>
    /// <returns>deserialized type or null</returns>
    Task<T> TryDeserializeFile<T>(string filePath) where T : class, new();
}