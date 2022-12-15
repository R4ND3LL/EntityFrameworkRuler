using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Extension;

/// <summary>
/// Hack to deal with lack of 'readonly collection' support when deserializing json.
/// Watch this issue for details and adjust accordingly: https://github.com/dotnet/runtime/issues/78556
/// </summary>
internal sealed class ReadOnlyCollectionConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) =>
        !typeToConvert.IsAbstract &&
        typeToConvert.GetConstructor(Type.EmptyTypes) != null &&
        typeToConvert
            .GetProperties()
            .Where(x => !x.CanWrite || x.GetSetMethod() == null)
            .Where(x => x.PropertyType.IsGenericType)
            .Select(x => new {
                Property = x,
                CollectionInterface = x.PropertyType.GetGenericInterfaces(typeof(ICollection<>)).FirstOrDefault()
            })
            .Any(x => x.CollectionInterface != null);

    private readonly Dictionary<Type, object> converterCache = new();

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        return (JsonConverter)converterCache.GetOrAddNew(typeToConvert, Factory);

        object Factory(Type arg) {
            return Activator.CreateInstance(typeof(ReadOnlyCollectionConverter<>).MakeGenericType(typeToConvert))!;
        }
    }

    private sealed class ReadOnlyCollectionConverter<T> : JsonConverter<T> where T : new() {
        private readonly Dictionary<string, (Type PropertyType, Action<T, object> Setter, Action<T, object> Adder)> propertyHandlers;

        public ReadOnlyCollectionConverter() {
            propertyHandlers = typeof(T)
                .GetProperties()
                .Where(o => o.Name != "Item")
                .Select(x => new {
                    Property = x,
                    CollectionInterface = (!x.CanWrite || x.GetSetMethod() == null) && x.PropertyType.IsGenericType
                        ? x.PropertyType.GetGenericInterfaces(typeof(ICollection<>)).FirstOrDefault()
                        : null
                })
                .Select(x => {
                    var tParam = Expression.Parameter(typeof(T));
                    var objParam = Expression.Parameter(typeof(object));
                    Action<T, object> setter = null;
                    Action<T, object> adder = null;
                    Type propertyType = null;
                    if (x.Property.CanWrite && x.Property.GetSetMethod() != null) {
                        // default deserialization
                        propertyType = x.Property.PropertyType;
                        setter = Expression.Lambda<Action<T, object>>(
                                Expression.Assign(
                                    Expression.Property(tParam, x.Property),
                                    Expression.Convert(objParam, propertyType)),
                                tParam,
                                objParam)
                            .Compile();
                    } else {
                        // custom readonly collection deserialization (just use Add)
                        // ReSharper disable once InvertIf
                        if (x.CollectionInterface != null) {
                            propertyType = x.CollectionInterface.GetGenericArguments()[0];
                            adder = Expression.Lambda<Action<T, object>>(
                                    Expression.Call(
                                        Expression.Property(tParam, x.Property),
                                        x.CollectionInterface.GetMethod("Add") ?? throw new("Add not found"),
                                        Expression.Convert(objParam, propertyType)),
                                    tParam,
                                    objParam)
                                .Compile();
                        }
                    }

                    return new { x.Property.Name, setter, adder, propertyType };
                })
                .Where(x => x.propertyType != null)
                .ToDictionary(x => x.Name, x => (x.propertyType!, x.setter, x.adder));
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => throw new NotSupportedException();

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var item = new T();
            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) break;

                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var propName = reader.GetString();
                if (propName == null) continue;
                var handler = propertyHandlers.TryGetValue(propName);
                if (handler.PropertyType == null) {
                    if (typeToConvert == typeof(SchemaRule)) {
                        if (propName == "Tables") {
                            propName = "Entities";
                            handler = propertyHandlers.TryGetValue(propName);
                        }
                    } else if (typeToConvert == typeof(EntityRule)) {
                        if (propName == "Columns") {
                            propName = "Properties";
                            handler = propertyHandlers.TryGetValue(propName);
                        }
                    } else {
#if DEBUG
                        if (Debugger.IsAttached) Debugger.Break();
#endif
                    }
                }

                if (handler.PropertyType != null) {
                    if (!reader.Read()) throw new JsonException($"Bad JSON");

                    if (handler.Setter != null)
                        handler.Setter(item, JsonSerializer.Deserialize(ref reader, handler.PropertyType, options));
                    else {
                        if (reader.TokenType == JsonTokenType.StartArray)
                            while (true) {
                                if (!reader.Read()) throw new JsonException($"Bad JSON");

                                if (reader.TokenType == JsonTokenType.EndArray) break;

                                handler.Adder!(item, JsonSerializer.Deserialize(ref reader, handler.PropertyType, options));
                            }
                        else
                            reader.Skip();
                    }
                } else
                    reader.Skip();
            }

            return item;
        }
    }
}