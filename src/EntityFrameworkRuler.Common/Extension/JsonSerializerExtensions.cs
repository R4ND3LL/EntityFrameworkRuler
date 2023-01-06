using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using EntityFrameworkRuler.Common;
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
    };

    private static readonly JsonSerializerOptions writeOptions = new() {
        WriteIndented = true,
        IgnoreReadOnlyProperties = false,
        TypeInfoResolver = new DataContractResolver {
            Modifiers = {
                ModifyWriteTypeInfo
            }
        }
    };

    private static void ModifyWriteTypeInfo(JsonTypeInfo ti) {
        if (ti.Kind != JsonTypeInfoKind.Object) return;

        if (typeof(RuleBase).IsAssignableFrom(ti.Type)) ShouldSerializeNotEmptyProperty(nameof(RuleBase.Annotations));

        if (ti.Type == typeof(DbContextRule)) {
            ShouldSerializeNotEmptyProperty(nameof(DbContextRule.Schemas));
            ShouldSerializeNotEmptyProperty(nameof(DbContextRule.ForeignKeys));
        }

        if (ti.Type == typeof(SchemaRule)) ShouldSerializeNotEmptyProperty(nameof(SchemaRule.Entities));

        if (ti.Type == typeof(EntityRule)) {
            ShouldSerializeNotEmptyProperty(nameof(EntityRule.Properties));
            ShouldSerializeNotEmptyProperty(nameof(EntityRule.Navigations));
        }

        if (ti.Type == typeof(PropertyRule)) ShouldSerializeNotEmptyProperty(nameof(PropertyRule.DiscriminatorConditions));

        // ReSharper disable once UnusedLocalFunctionReturnValue
        JsonPropertyInfo ShouldSerializeNotEmptyProperty(string propName) {
            return ShouldSerializeNotEmpty(ti.Properties.FirstOrDefault(o => o.Name == propName));
        }

        JsonPropertyInfo ShouldSerializeNotEmpty(JsonPropertyInfo p) {
            // ReSharper disable once UnusedParameter.Local
            p.ShouldSerialize = (parent, propValue) => propValue is ICollection c && c.Count > 0;
            return p;
        }
    }

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
        var result = ReadJson<T>(text);
        return result;
    }

    /// <summary> Read the json file or return NULL on failure </summary>
    /// <param name="text">json text to deserialize </param>
    /// <typeparam name="T">type to deserialize</typeparam>
    /// <returns>deserialized type or null</returns>
    public static T ReadJson<T>(this string text) where T : class {
        if (string.IsNullOrEmpty(text)) return null;
        var result = JsonSerializer.Deserialize<T>(text, readOptions);
        return result;
    }


    /// <summary> Serialize the given object to json text </summary>
    /// <param name="jsonModel"> object to serialize</param>
    /// <typeparam name="T"> exact type of given object </typeparam>
    /// <returns> json string </returns>
    public static string WriteJson<T>(this T jsonModel)
        where T : class {
        using var ms = new MemoryStream();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(jsonModel, jsonModel.GetType(), writeOptions);
        return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    /// <summary> Serialize the given object to json text and write to the given file path </summary>
    /// <param name="jsonModel"> object to serialize</param>
    /// <param name="filePath"> target path to write serialized json string </param>
    /// <typeparam name="T"> exact type of given object </typeparam>
    public static async Task WriteJsonFile<T>(this T jsonModel, string filePath)
        where T : class {
        if (filePath.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filePath));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(jsonModel, jsonModel.GetType(), writeOptions);
#if !LEGACY
        await File.WriteAllBytesAsync(filePath, bytes);
#else
        if (filePath.Length == int.MaxValue) await Task.Delay(0);
        File.WriteAllBytes(filePath, bytes);
#endif
        // using var fs = File.Open(filePath, FileMode.Create);
        // // ReSharper disable once UseAwaitUsing
        // using var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true, "   ");
        // var serializer = new DataContractJsonSerializer(typeof(T));
        // serializer.WriteObject(writer, jsonModel);
        // // ReSharper disable once MethodHasAsyncOverload
        // writer.Flush();
    }
}