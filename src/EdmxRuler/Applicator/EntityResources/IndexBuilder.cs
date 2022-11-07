namespace Microsoft.EntityFrameworkCore.Metadata.Builders; 

public class IndexBuilder<T> {
    public IndexBuilder(IMutableIndex index) {
    }

    public virtual IndexBuilder<T> HasAnnotation(string annotation, object? value) {
        return this;
    }

    public virtual IndexBuilder<T> IsUnique(bool unique = true) {
        return this;
    }
}