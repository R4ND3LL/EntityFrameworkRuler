using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EntityFrameworkRuler.Rules;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Extension;

/// <summary> Json serialization helpers </summary>
public static class JsonSerializerExtensions {
    private static readonly JsonSerializerOptions readOptions = new() {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        IgnoreReadOnlyProperties = false,
        Converters = { new ReadOnlyCollectionConverterFactory(), new NavigationConverterFactory() }
    };

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
            var text = await File.ReadAllTextAsync(filePath);
            var result = JsonSerializer.Deserialize<T>(text, readOptions);
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
        await using var fs = File.Open(filePath, FileMode.Create);
        using var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true, "   ");
        var serializer = new DataContractJsonSerializer(typeof(T));
        serializer.WriteObject(writer, jsonModel);
        // ReSharper disable once MethodHasAsyncOverload
        writer.Flush();
    }
}