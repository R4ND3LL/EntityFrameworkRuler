using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace EntityFrameworkRuler.Common {
    internal sealed class DataContractResolver : IJsonTypeInfoResolver {
        #region static members

        private static DataContractResolver defaultInstance;

        public static DataContractResolver Default {
            get {
                if (defaultInstance is { } result) return result;

                DataContractResolver newInstance = new();
                var originalInstance = Interlocked.CompareExchange(ref defaultInstance, newInstance, comparand: null);
                return originalInstance ?? newInstance;
            }
        }

        private static bool IsNullOrDefault(object obj) {
            if (obj is null) return true;

            var type = obj.GetType();

            return type.IsValueType && FormatterServices.GetUninitializedObject(type).Equals(obj);
        }

        private static IEnumerable<MemberInfo> EnumerateFieldsAndProperties(Type type, BindingFlags bindingFlags) {
            foreach (var fieldInfo in type.GetFields(bindingFlags)) yield return fieldInfo;

            foreach (var propertyInfo in type.GetProperties(bindingFlags)) yield return propertyInfo;
        }

        private static IEnumerable<JsonPropertyInfo> CreateDataMembers(JsonTypeInfo jsonTypeInfo) {
            var isDataContract = jsonTypeInfo.Type.GetCustomAttribute<DataContractAttribute>() != null;
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            if (isDataContract) bindingFlags |= BindingFlags.NonPublic;

            foreach (var memberInfo in EnumerateFieldsAndProperties(jsonTypeInfo.Type, bindingFlags)) {
                DataMemberAttribute attr = null;
                if (isDataContract) {
                    attr = memberInfo.GetCustomAttribute<DataMemberAttribute>();
                    if (attr == null) continue;
                } else {
                    if (memberInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() != null) continue;
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // ReSharper disable once HeuristicUnreachableCode
                if (memberInfo == null) continue;

                Func<object, object> getValue = null;
                Action<object, object> setValue = null;
                Type propertyType;
                string propertyName;

                if (memberInfo.MemberType == MemberTypes.Field && memberInfo is FieldInfo fieldInfo) {
                    propertyName = attr?.Name ?? fieldInfo.Name;
                    propertyType = fieldInfo.FieldType;
                    getValue = fieldInfo.GetValue;
                    setValue = (obj, value) => fieldInfo.SetValue(obj, value);
                } else if (memberInfo.MemberType == MemberTypes.Property && memberInfo is PropertyInfo propertyInfo) {
                    propertyName = attr?.Name ?? propertyInfo.Name;
                    propertyType = propertyInfo.PropertyType;
                    if (propertyInfo.CanRead) getValue = propertyInfo.GetValue;

                    if (propertyInfo.CanWrite) setValue = (obj, value) => propertyInfo.SetValue(obj, value);
                } else
                    continue;

                var jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(propertyType, propertyName);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // ReSharper disable once HeuristicUnreachableCode
                if (jsonPropertyInfo == null) continue;

                jsonPropertyInfo.Get = getValue;
                jsonPropertyInfo.Set = setValue;

                if (attr != null) {
                    jsonPropertyInfo.Order = attr.Order;
                    jsonPropertyInfo.ShouldSerialize = !attr.EmitDefaultValue ? (_, obj) => !IsNullOrDefault(obj) : null;
                }

                yield return jsonPropertyInfo;
            }
        }

        public static JsonTypeInfo GetTypeInfo(JsonTypeInfo jsonTypeInfo) {
            if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object) {
                jsonTypeInfo.CreateObject = () => Activator.CreateInstance(jsonTypeInfo.Type)!;

                foreach (var jsonPropertyInfo in CreateDataMembers(jsonTypeInfo).OrderBy((x) => x.Order))
                    jsonTypeInfo.Properties.Add(jsonPropertyInfo);
            }

            return jsonTypeInfo;
        }

        #endregion

        private IList<Action<JsonTypeInfo>> modifiers;

        /// <summary>Gets a list of user-defined callbacks that can be used to modify the initial contract.</summary>
        public IList<Action<JsonTypeInfo>> Modifiers => modifiers ??= new List<Action<JsonTypeInfo>>();

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
            var jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, options);

            var info = GetTypeInfo(jsonTypeInfo);
            if (modifiers?.Count > 0)
                foreach (var modifier in modifiers)
                    modifier(jsonTypeInfo);
            return info;
        }
    }
}