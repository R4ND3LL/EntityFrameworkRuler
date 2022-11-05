using System.Runtime.Serialization.Json;
using System.Text;

namespace EdmxRuler.Extensions;

internal static class JsonSerializerExtensions {
    public static async Task<T> TryReadJsonFileOrNew<T>(this string filePath)
        where T : class, new() {
        return await TryReadJsonFile<T>(filePath) ?? new T();
    }

    public static async Task<T> TryReadJsonFile<T>(this string filePath) where T : class {
        try {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
            var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            var ser = new DataContractJsonSerializer(typeof(T));
            ms.Close();
            return ser.ReadObject(ms) as T;
        } catch {
            return null;
        }
    }

    public static string ToJson<T>(this T jsonModelRoot)
        where T : class {
        using var ms = new MemoryStream();
        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true, "   ")) {
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(writer, jsonModelRoot);
            writer.Flush();
        }

        var json = ms.ToArray();
        return Encoding.UTF8.GetString(json, 0, json.Length);
    }

    public static async Task ToJson<T>(this T jsonModel, string filePath)
        where T : class {
        var json = ToJson(jsonModel);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }
}