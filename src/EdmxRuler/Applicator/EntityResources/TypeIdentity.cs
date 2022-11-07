// ReSharper disable CheckNamespace

namespace Microsoft.EntityFrameworkCore.Metadata.Internal {
    public class TypeIdentity : IEquatable<TypeIdentity> {
        public TypeIdentity(string name) { }
        public TypeIdentity(string name, Type type) { }
        public TypeIdentity(Type type, Model model) { }
        public string Name { get; }
        public Type? Type { get; }
        public bool IsNamed { get; }
        public bool Equals(TypeIdentity other) { return false; }
    }
}