using System.Linq.Expressions;
using EdmxRuler.Generator.EdmxModel;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

// ReSharper disable CheckNamespace

namespace Microsoft.EntityFrameworkCore.Metadata.Builders {
    public interface IConventionEntityTypeBuilder {
    }

    public class OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> : OwnedNavigationBuilder
        where TOwnerEntity : class
        where TDependentEntity : class {
        public OwnedNavigationBuilder(IMutableForeignKey ownership)
            : base(ownership) {
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> HasAnnotation(
            string annotation,
            object? value) {
            return default;
        }

        public virtual KeyBuilder<TDependentEntity> HasKey(
            Expression<Func<TDependentEntity, object>> keyExpression) {
            return default;
        }

        public virtual KeyBuilder<TDependentEntity> HasKey(params string[] propertyNames) {
            return default;
        }

        public virtual PropertyBuilder<TProperty> Property<TProperty>(
            Expression<Func<TDependentEntity, TProperty>> propertyExpression) {
            return default;
        }

        public virtual NavigationBuilder<TDependentEntity, TNavigation> Navigation<TNavigation>(
            Expression<Func<TDependentEntity, TNavigation?>> navigationExpression)
            where TNavigation : class {
            return default;
        }

        public virtual NavigationBuilder<TDependentEntity, TNavigation> Navigation<TNavigation>(
            Expression<Func<TDependentEntity, IEnumerable<TNavigation>?>> navigationExpression)
            where TNavigation : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> Ignore(
            string propertyName) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> Ignore(
            Expression<Func<TDependentEntity, object?>> propertyExpression) {
            return default;
        }

        public virtual IndexBuilder<TDependentEntity> HasIndex(
            Expression<Func<TDependentEntity, object?>> indexExpression) {
            return default;
        }

        public virtual IndexBuilder<TDependentEntity> HasIndex(params string[] propertyNames) {
            return default;
        }

        public virtual OwnershipBuilder<TOwnerEntity, TDependentEntity> WithOwner(
            string? ownerReference = null) {
            return default;
        }

        public virtual OwnershipBuilder<TOwnerEntity, TDependentEntity> WithOwner(
            Expression<Func<TDependentEntity, TOwnerEntity?>>? referenceExpression) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity> OwnsOne<TNewDependentEntity>(
            string navigationName)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity> OwnsOne<TNewDependentEntity>(
            string ownedTypeName,
            string navigationName)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity> OwnsOne<TNewDependentEntity>(
            Expression<Func<TDependentEntity, TNewDependentEntity?>> navigationExpression)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity> OwnsOne<TNewDependentEntity>(
            string ownedTypeName,
            Expression<Func<TDependentEntity, TNewDependentEntity?>> navigationExpression)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsOne<TNewDependentEntity>(
            string navigationName,
            Action<OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity>> buildAction)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsOne(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsOne(
            string ownedTypeName,
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsOne(
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsOne<TNewDependentEntity>(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity>> buildAction)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsOne<TNewDependentEntity>(
            Expression<Func<TDependentEntity, TNewDependentEntity?>> navigationExpression,
            Action<OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity>> buildAction)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsOne<TNewDependentEntity>(
            string ownedTypeName,
            Expression<Func<TDependentEntity, TNewDependentEntity?>> navigationExpression,
            Action<OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity>> buildAction)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity> OwnsMany<TNewDependentEntity>(
            string navigationName)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity> OwnsMany<TNewDependentEntity>(
            string ownedTypeName,
            string navigationName)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity> OwnsMany<TNewDependentEntity>(
            Expression<Func<TDependentEntity, IEnumerable<TNewDependentEntity>?>> navigationExpression)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity> OwnsMany<TNewDependentEntity>(
            string ownedTypeName,
            Expression<Func<TDependentEntity, IEnumerable<TNewDependentEntity>?>> navigationExpression)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsMany<TNewDependentEntity>(
            string navigationName,
            Action<OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity>> buildAction)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsMany(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsMany(
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsMany(
            string ownedTypeName,
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsMany<TNewDependentEntity>(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity>> buildAction)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsMany<TNewDependentEntity>(
            Expression<Func<TDependentEntity, IEnumerable<TNewDependentEntity>?>> navigationExpression,
            Action<OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity>> buildAction)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> OwnsMany<TNewDependentEntity>(
            string ownedTypeName,
            Expression<Func<TDependentEntity, IEnumerable<TNewDependentEntity>?>> navigationExpression,
            Action<OwnedNavigationBuilder<TDependentEntity, TNewDependentEntity>> buildAction)
            where TNewDependentEntity : class {
            return default;
        }

        public virtual ReferenceNavigationBuilder<TDependentEntity, TNewRelatedEntity> HasOne<TNewRelatedEntity>(
            string? navigationName)
            where TNewRelatedEntity : class {
            return default;
        }

        public virtual ReferenceNavigationBuilder<TDependentEntity, TNewRelatedEntity> HasOne<TNewRelatedEntity>(
            Expression<Func<TDependentEntity, TNewRelatedEntity?>>? navigationExpression = null)
            where TNewRelatedEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> HasChangeTrackingStrategy(
            ChangeTrackingStrategy changeTrackingStrategy) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> UsePropertyAccessMode(
            PropertyAccessMode propertyAccessMode) {
            return default;
        }

        public virtual DataBuilder<TDependentEntity> HasData(params TDependentEntity[] data) {
            return default;
        }

        public virtual DataBuilder<TDependentEntity> HasData(
            IEnumerable<TDependentEntity> data) {
            return default;
        }

        public virtual DataBuilder<TDependentEntity> HasData(params object[] data) {
            return default;
        }

        public virtual DataBuilder<TDependentEntity> HasData(IEnumerable<object> data) {
            return default;
        }
    }

    public enum ChangeTrackingStrategy {
    }

    public interface IMutableForeignKey {
    }

    public class OwnedNavigationBuilder {
        public OwnedNavigationBuilder(IMutableForeignKey ownership) {
        }

        public virtual IMutableForeignKey Metadata => default;
        public virtual IMutableEntityType OwnedEntityType => default;

        public virtual OwnedNavigationBuilder HasAnnotation(
            string annotation,
            object? value) {
            return default;
        }

        public virtual KeyBuilder HasKey(params string[] propertyNames) {
            return default;
        }

        public virtual PropertyBuilder Property(string propertyName) {
            return default;
        }

        public virtual PropertyBuilder<TProperty> Property<TProperty>(string propertyName) {
            return default;
        }

        public virtual PropertyBuilder Property(Type propertyType, string propertyName) {
            return default;
        }

        public virtual PropertyBuilder<TProperty> IndexerProperty<TProperty>(
            string propertyName) {
            return default;
        }

        public virtual PropertyBuilder IndexerProperty(
            Type propertyType,
            string propertyName) {
            return default;
        }

        public virtual NavigationBuilder Navigation(string navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder Ignore(string propertyName) {
            return default;
        }

        public virtual IndexBuilder HasIndex(params string[] propertyNames) {
            return default;
        }

        public virtual OwnershipBuilder WithOwner(string? ownerReference = null) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsOne(
            string ownedTypeName,
            string navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsOne(
            string ownedTypeName,
            Type ownedType,
            string navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsOne(
            Type ownedType,
            string navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsOne(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsOne(
            string ownedTypeName,
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsOne(
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        private OwnedNavigationBuilder OwnsOneBuilder(
            in TypeIdentity ownedType,
            string navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsMany(
            string ownedTypeName,
            string navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsMany(
            string ownedTypeName,
            Type ownedType,
            string navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsMany(
            Type ownedType,
            string navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsMany(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsMany(
            string ownedTypeName,
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual OwnedNavigationBuilder OwnsMany(
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        private OwnedNavigationBuilder OwnsManyBuilder(
            in TypeIdentity ownedType,
            string navigationName) {
            return default;
        }

        public virtual ReferenceNavigationBuilder HasOne(
            string relatedTypeName,
            string? navigationName) {
            return default;
        }

        public virtual ReferenceNavigationBuilder HasOne(string navigationName) {
            return default;
        }

        public virtual ReferenceNavigationBuilder HasOne(
            Type relatedType,
            string? navigationName = null) {
            return default;
        }

        protected virtual EntityType FindRelatedEntityType(
            string relatedTypeName,
            string? navigationName) {
            return default;
        }

        protected virtual EntityType FindRelatedEntityType(
            Type relatedType,
            string? navigationName) {
            return default;
        }

        public virtual OwnedNavigationBuilder HasChangeTrackingStrategy(
            ChangeTrackingStrategy changeTrackingStrategy) {
            return default;
        }

        public virtual OwnedNavigationBuilder UsePropertyAccessMode(
            PropertyAccessMode propertyAccessMode) {
            return default;
        }

        public virtual DataBuilder HasData(params object[] data) {
            return default;
        }

        public virtual DataBuilder HasData(IEnumerable<object> data) {
            return default;
        }
    }
}