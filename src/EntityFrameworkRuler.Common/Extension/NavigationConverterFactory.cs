using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Extension;

/// <summary>
/// For backward compatibility of NavigationRule.Name property when it was a List type.
/// Will read the first string from the list and use that as the Name value.
/// </summary>
internal sealed class NavigationConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(NavigationRule);

    private NavigationConverter converterCache;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        return converterCache ??= new NavigationConverter();
    }

    private sealed class NavigationConverter : JsonConverter<NavigationRule> {
        private readonly
            Dictionary<string, (Type PropertyType, Action<NavigationRule, object> Setter, Action<NavigationRule, object> Adder)>
            propertyHandlers;

        public NavigationConverter() {
            propertyHandlers = typeof(NavigationRule)
                .GetProperties()
                .Select(x => new {
                    Property = x,
                    CollectionInterface = (!x.CanWrite || x.GetSetMethod() == null) && x.PropertyType.IsGenericType
                        ? x.PropertyType.GetGenericInterfaces(typeof(ICollection<>)).FirstOrDefault()
                        : null
                })
                .Select(x => {
                    var tParam = Expression.Parameter(typeof(NavigationRule));
                    var objParam = Expression.Parameter(typeof(object));
                    Action<NavigationRule, object> setter = null;
                    Action<NavigationRule, object> adder = null;
                    Type propertyType = null;
                    if (x.Property.CanWrite && x.Property.GetSetMethod() != null) {
                        propertyType = x.Property.PropertyType;
                        if (x.Property.Name !=nameof(NavigationRule.Name)) {
                            // default deserialization
                            setter = Expression.Lambda<Action<NavigationRule, object>>(
                                    Expression.Assign(
                                        Expression.Property(tParam, x.Property),
                                        Expression.Convert(objParam, propertyType)),
                                    tParam,
                                    objParam)
                                .Compile();
                        } else {
                            // custom collection deserialization
                            adder = NameFromList;
                        }
                    }

                    return new { x.Property.Name, setter, adder, propertyType };
                })
                .Where(x => x.propertyType != null)
                .ToDictionary(x => x.Name, x => (x.propertyType!, x.setter, x.adder));
        }

        private void NameFromList(NavigationRule r, object obj) {
            switch (obj) {
                case null:
                    return;
                case string s when s.HasNonWhiteSpace():
                    r.Name = s;
                    break;
                default:
                    throw new Exception($"Unsupported Name value: {obj.GetType().Name}");
            }
        }

        public override void Write(Utf8JsonWriter writer, NavigationRule value, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override NavigationRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var item = new NavigationRule();
            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) break;

                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var propName = reader.GetString();
                if (propName == null) continue;
                if (propertyHandlers.TryGetValue(propName, out var handler)) {
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
                        if (reader.TokenType == JsonTokenType.String)
                            handler.Adder!(item, JsonSerializer.Deserialize(ref reader, handler.PropertyType, options));
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