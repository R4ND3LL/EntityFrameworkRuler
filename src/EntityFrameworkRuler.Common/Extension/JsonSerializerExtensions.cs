using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
        Converters = { new ReadOnlyCollectionConverterFactory(), new NavigationConverterFactory() },
        // TypeInfoResolver = new DefaultJsonTypeInfoResolver {
        //     Modifiers = {
        //         ModifyTypeInfo
        //     }
        // }
    };

    // private static void ModifyTypeInfo(JsonTypeInfo ti) {
    //     // if (ti.Kind != JsonTypeInfoKind.None) return;
    //     // if (ti.Type == typeof(SchemaRule)) {
    //     //     var tables = ti.Properties.FirstOrDefault(o => o.Name == "Entities");
    //     //     if (tables == null) {
    //     //         //ti.Kind = JsonTypeInfoKind.Object;
    //     //         //ti.Properties.Add(ti.CreateJsonPropertyInfo(typeof(EntityRule), "Entities"));
    //     //     }
    //     // }
    //     //
    //     // if (ti.Type == typeof(EntityRule)) {
    //     //     var properties = ti.Properties.FirstOrDefault(o => o.Name == "Properties");
    //     //     if (properties == null) {
    //     //         //ti.Properties.Add(ti.CreateJsonPropertyInfo(typeof(EntityRule), "Entities"));
    //     //     }
    //     // }
    // }

    /// <summary> Read the json file or return NULL on failure </summary>
    /// <param name="filePath">json file path to load</param>
    /// <typeparam name="T">type to deserialize</typeparam>
    /// <returns>deserialized type or null</returns>
    public static async Task<T> ReadJsonFile<T>(this string filePath) where T : class {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
#if LEGACY
        if (filePath.Length == int.MaxValue) await Task.Delay(0); // just to make async method happy
        var text = File.ReadAllText(filePath);
#else
        var text = await File.ReadAllTextAsync(filePath);
#endif
        var result = JsonSerializer.Deserialize<T>(text, readOptions);
        return result;
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
    public static async Task WriteJsonFile<T>(this T jsonModel, string filePath)
        where T : class {
        if (filePath.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filePath));
#if !LEGACY
        await
#else
        if (filePath.Length == int.MaxValue) await Task.Delay(0);
#endif
            using var fs = File.Open(filePath, FileMode.Create);
        // ReSharper disable once UseAwaitUsing
        using var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true, "   ");
        var serializer = new DataContractJsonSerializer(typeof(T));
        serializer.WriteObject(writer, jsonModel);
        // ReSharper disable once MethodHasAsyncOverload
        writer.Flush();
    }
}