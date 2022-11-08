// ReSharper disable CheckNamespace

namespace Microsoft.EntityFrameworkCore.Metadata.Builders {
    public class IndexBuilder<T> : IndexBuilder {
        public virtual IndexBuilder<T> HasAnnotation(string annotation, object value) {
            return this;
        }

        public virtual IndexBuilder<T> IsUnique(bool unique = true) {
            return this;
        }
    }

    public class IndexBuilder {
    }
}