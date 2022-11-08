// ReSharper disable CheckNamespace

namespace Microsoft.EntityFrameworkCore.Metadata.Builders {
    public interface IMutableKey {
    }

    public class KeyBuilder<T> : KeyBuilder {
        public KeyBuilder(IMutableKey key)
            : base(key) {
        }

        public virtual KeyBuilder<T> HasAnnotation(string annotation, object value) {
            return (KeyBuilder<T>)base.HasAnnotation(annotation, value);
        }
    }

    public class KeyBuilder {
        public KeyBuilder(IMutableKey key) {
        }

        public virtual KeyBuilder HasAnnotation(string annotation, object value) {
            return default;
        }
    }
}