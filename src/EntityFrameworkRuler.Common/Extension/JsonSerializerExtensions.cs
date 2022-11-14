using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Extension;

public static class JsonSerializerExtensions {
    /// <summary> Read the json file or return new instance on failure </summary>
    /// <param name="filePath">json file path to load</param>
    /// <typeparam name="T">type to deserialize</typeparam>
    /// <returns>deserialized type</returns>
    public static async Task<T> TryReadJsonFileOrNew<T>(this string filePath)
        where T : class, new() {
        return await TryReadJsonFile<T>(filePath) ?? new T();
    }

    /// <summary> Read the json file or return NULL on failure </summary>
    /// <param name="filePath">json file path to load</param>
    /// <typeparam name="T">type to deserialize</typeparam>
    /// <returns>deserialized type or null</returns>
    public static async Task<T> TryReadJsonFile<T>(this string filePath) where T : class {
        try {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
            var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            var ser = new DataContractJsonSerializer(typeof(T));
            var result = ser.ReadObject(ms) as T;
            ms.Close();
            return result;
        } catch {
            return null;
        }
    }

    /// <summary> Serialize the given object to json text </summary>
    /// <param name="jsonModel"> object to serialize</param>
    /// <typeparam name="T"> exact type of given object </typeparam>
    /// <returns> json string </returns>
    public static string ToJson<T>(this T jsonModel)
        where T : class {
        using var ms = new MemoryStream();
        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true, "   ")) {
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(writer, jsonModel);
            writer.Flush();
        }

        var json = ms.ToArray();
        return Encoding.UTF8.GetString(json, 0, json.Length);
    }

    /// <summary> Serialize the given object to json text and write to the given file path </summary>
    /// <param name="jsonModel"> object to serialize</param>
    /// <param name="filePath"> target path to write serialized json string </param>
    /// <typeparam name="T"> exact type of given object </typeparam>
    public static async Task ToJson<T>(this T jsonModel, string filePath)
        where T : class {
        var json = ToJson(jsonModel);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }
}