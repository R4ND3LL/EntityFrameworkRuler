// ReSharper disable CheckNamespace

namespace Microsoft.EntityFrameworkCore.Metadata.Builders {
    public interface IMutableNavigationBase {
    }

    public class NavigationBuilder<TSource, TTarget> : NavigationBuilder
        where TSource : class
        where TTarget : class {
        public NavigationBuilder(IMutableNavigationBase navigationOrSkipNavigation)
            : base(navigationOrSkipNavigation) {
        }

        public virtual NavigationBuilder<TSource, TTarget> HasAnnotation(
            string annotation,
            object value) {
            return default;
        }

        public virtual NavigationBuilder<TSource, TTarget> UsePropertyAccessMode(
            PropertyAccessMode propertyAccessMode) {
            return default;
        }

        public virtual NavigationBuilder<TSource, TTarget> HasField(string fieldName) {
            return default;
        }

        public virtual NavigationBuilder<TSource, TTarget> AutoInclude(bool autoInclude = true) {
            return default;
        }

        public virtual NavigationBuilder<TSource, TTarget> IsRequired(bool required = true) {
            return default;
        }
    }

    public enum PropertyAccessMode { }

    public class NavigationBuilder {
        public NavigationBuilder(IMutableNavigationBase navigationOrSkipNavigation) {
        }

        public virtual IMutableNavigationBase Metadata { get; }

        public virtual NavigationBuilder HasAnnotation(string annotation, object value) {
            return default;
        }

        public virtual NavigationBuilder UsePropertyAccessMode(
            PropertyAccessMode propertyAccessMode) {
            return default;
        }

        public virtual NavigationBuilder HasField(string fieldName) {
            return default;
        }

        public virtual NavigationBuilder AutoInclude(bool autoInclude = true) {
            return default;
        }

        public virtual NavigationBuilder IsRequired(bool required = true) {
            return default;
        }
    }
}