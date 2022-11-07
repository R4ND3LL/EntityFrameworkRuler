// ReSharper disable CheckNamespace

using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.ChangeTracking {
    public enum EntityState { }

    public class MemberEntry {
    }

    public class CollectionEntry<TEntity, TRelatedEntity> : CollectionEntry
        where TEntity : class
        where TRelatedEntity : class {
        public new virtual EntityEntry<TEntity> EntityEntry
            => default;

        public new virtual IEnumerable<TRelatedEntity>? CurrentValue {
            get => default;
            set { }
        }

        public new virtual IQueryable<TRelatedEntity> Query() => default;
        public new virtual EntityEntry<TRelatedEntity>? FindEntry(object entity) => default;
    }

    public class CollectionEntry : NavigationEntry {
    }

    public class ReferenceEntry<TEntity, TProperty> : ReferenceEntry
        where TEntity : class
        where TProperty : class {
        public new virtual EntityEntry<TEntity> EntityEntry => default;

        public new virtual EntityEntry<TProperty>? TargetEntry => default;

        public new virtual TProperty? CurrentValue => default;

        public new virtual IQueryable<TProperty> Query() => default;
    }

    public class ReferenceEntry : NavigationEntry {
    }

    public class PropertyEntry<TEntity, TProperty> : PropertyEntry
        where TEntity : class {
        public new virtual EntityEntry<TEntity> EntityEntry
            => default;

        public new virtual TProperty CurrentValue
            => default;

        public new virtual TProperty OriginalValue
            => default;
    }

    public class PropertyEntry {
    }

    public class NavigationEntry {
    }

    public class PropertyValues {
    }

    public class EntityEntry<TEntity> : EntityEntry
        where TEntity : class {
        public new virtual TEntity Entity
            => default;

        public virtual PropertyEntry<TEntity, TProperty> Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression) => default;

        public virtual ReferenceEntry<TEntity, TProperty> Reference<TProperty>(
            Expression<Func<TEntity, TProperty?>> propertyExpression)
            where TProperty : class => default;

        public virtual CollectionEntry<TEntity, TProperty> Collection<TProperty>(
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression)
            where TProperty : class => default;

        public virtual ReferenceEntry<TEntity, TProperty> Reference<TProperty>(string propertyName)
            where TProperty : class => default;

        public virtual CollectionEntry<TEntity, TProperty> Collection<TProperty>(string propertyName)
            where TProperty : class => default;

        public virtual PropertyEntry<TEntity, TProperty> Property<TProperty>(
            string propertyName) => default;
    }

    public class EntityEntry {
        public virtual object Entity => default;
        public virtual EntityState State => default;

        public virtual void DetectChanges() {
        }


        public virtual DbContext Context => default;
        public virtual IEntityType Metadata => default;
        public virtual MemberEntry Member(string propertyName) => default;

        public virtual IEnumerable<MemberEntry> Members => default;
        public virtual NavigationEntry Navigation(string propertyName) => default;
        public virtual IEnumerable<NavigationEntry> Navigations => default;
        public virtual PropertyEntry Property(string propertyName) => default;
        public virtual IEnumerable<PropertyEntry> Properties => default;
        public virtual ReferenceEntry Reference(string propertyName) => default;
        public virtual IEnumerable<ReferenceEntry> References => default;
        public virtual CollectionEntry Collection(string propertyName) => default;
        public virtual IEnumerable<CollectionEntry> Collections => default;
        public virtual bool IsKeySet => default;
        public virtual PropertyValues CurrentValues => default;
        public virtual PropertyValues OriginalValues => default;
        public virtual PropertyValues? GetDatabaseValues() => default;

        public virtual Task<PropertyValues?> GetDatabaseValuesAsync(CancellationToken cancellationToken = default) =>
            default;

        public virtual void Reload() { }

        public virtual Task ReloadAsync(CancellationToken cancellationToken = default)
            => default;

        public virtual object DebugView => default;
    }
}