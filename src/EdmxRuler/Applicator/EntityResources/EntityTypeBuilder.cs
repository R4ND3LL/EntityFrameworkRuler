using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

// ReSharper disable CheckNamespace

namespace Microsoft.EntityFrameworkCore.Metadata.Builders {
    public class EntityTypeBuilder<TEntity> : EntityTypeBuilder where TEntity : class {
        public EntityTypeBuilder(IMutableEntityType entityType)
            : base(entityType) {
        }

        public virtual EntityTypeBuilder<TEntity> HasAnnotation(
            string annotation,
            object? value) {
            return this;
        }

        public virtual EntityTypeBuilder<TEntity> HasBaseType(string? name) {
            return this;
        }

        public virtual EntityTypeBuilder<TEntity> HasBaseType(Type? entityType) {
            return this;
        }

        public virtual EntityTypeBuilder<TEntity> HasBaseType<TBaseType>() {
            return this;
        }

        public virtual KeyBuilder HasKey(Expression<Func<TEntity, object?>> keyExpression) {
            return default;
        }

        public virtual KeyBuilder<TEntity> HasKey(params string[] propertyNames) {
            return default;
        }

        public virtual KeyBuilder<TEntity> HasAlternateKey(
            Expression<Func<TEntity, object?>> keyExpression) {
            return default;
        }

        public virtual KeyBuilder<TEntity> HasAlternateKey(params string[] propertyNames) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> HasNoKey() {
            return this;
        }

        public virtual PropertyBuilder<TProperty> Property<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression) {
            return default;
        }

        public virtual NavigationBuilder<TEntity, TNavigation> Navigation<TNavigation>(
            Expression<Func<TEntity, TNavigation?>> navigationExpression)
            where TNavigation : class {
            return default;
        }

        public virtual NavigationBuilder<TEntity, TNavigation> Navigation<TNavigation>(
            Expression<Func<TEntity, IEnumerable<TNavigation>?>> navigationExpression)
            where TNavigation : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> Ignore(
            Expression<Func<TEntity, object?>> propertyExpression) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> Ignore(string propertyName) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> HasQueryFilter(
            LambdaExpression? filter) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> HasQueryFilter(
            Expression<Func<TEntity, bool>>? filter) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> ToQuery(
            Expression<Func<IQueryable<TEntity>>> query) {
            return default;
        }

        public virtual IndexBuilder<TEntity> HasIndex(
            Expression<Func<TEntity, object?>> indexExpression) {
            return default;
        }

        public virtual IndexBuilder<TEntity> HasIndex(
            Expression<Func<TEntity, object?>> indexExpression,
            string name) {
            return default;
        }

        public virtual IndexBuilder<TEntity> HasIndex(params string[] propertyNames) {
            return default;
        }

        public virtual IndexBuilder<TEntity> HasIndex(string[] propertyNames, string name) {
            return default;
        }

        public virtual OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsOne<TRelatedEntity>(
            string navigationName)
            where TRelatedEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsOne<TRelatedEntity>(
            string ownedTypeName,
            string navigationName)
            where TRelatedEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsOne<TRelatedEntity>(
            Expression<Func<TEntity, TRelatedEntity?>> navigationExpression)
            where TRelatedEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsOne<TRelatedEntity>(
            string ownedTypeName,
            Expression<Func<TEntity, TRelatedEntity?>> navigationExpression)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsOne<TRelatedEntity>(
            string navigationName,
            Action<OwnedNavigationBuilder<TEntity, TRelatedEntity>> buildAction)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsOne(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsOne(
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsOne(
            string ownedTypeName,
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsOne<TRelatedEntity>(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder<TEntity, TRelatedEntity>> buildAction)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsOne<TRelatedEntity>(
            Expression<Func<TEntity, TRelatedEntity?>> navigationExpression,
            Action<OwnedNavigationBuilder<TEntity, TRelatedEntity>> buildAction)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsOne<TRelatedEntity>(
            string ownedTypeName,
            Expression<Func<TEntity, TRelatedEntity?>> navigationExpression,
            Action<OwnedNavigationBuilder<TEntity, TRelatedEntity>> buildAction)
            where TRelatedEntity : class {
            return default;
        }

        private OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsOneBuilder<TRelatedEntity>(
            TypeIdentity ownedType,
            MemberIdentity navigation)
            where TRelatedEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsMany<TRelatedEntity>(
            string navigationName)
            where TRelatedEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsMany<TRelatedEntity>(
            string ownedTypeName,
            string navigationName)
            where TRelatedEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsMany<TRelatedEntity>(
            Expression<Func<TEntity, IEnumerable<TRelatedEntity>?>> navigationExpression)
            where TRelatedEntity : class {
            return default;
        }

        public virtual OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsMany<TRelatedEntity>(
            string ownedTypeName,
            Expression<Func<TEntity, IEnumerable<TRelatedEntity>?>> navigationExpression)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsMany<TRelatedEntity>(
            string navigationName,
            Action<OwnedNavigationBuilder<TEntity, TRelatedEntity>> buildAction)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsMany(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsMany(
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsMany(
            string ownedTypeName,
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsMany<TRelatedEntity>(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder<TEntity, TRelatedEntity>> buildAction)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsMany<TRelatedEntity>(
            Expression<Func<TEntity, IEnumerable<TRelatedEntity>?>> navigationExpression,
            Action<OwnedNavigationBuilder<TEntity, TRelatedEntity>> buildAction)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> OwnsMany<TRelatedEntity>(
            string ownedTypeName,
            Expression<Func<TEntity, IEnumerable<TRelatedEntity>?>> navigationExpression,
            Action<OwnedNavigationBuilder<TEntity, TRelatedEntity>> buildAction)
            where TRelatedEntity : class {
            return default;
        }

        private OwnedNavigationBuilder<TEntity, TRelatedEntity> OwnsManyBuilder<TRelatedEntity>(
            TypeIdentity ownedType,
            MemberIdentity navigation)
            where TRelatedEntity : class {
            return default;
        }

        public virtual ReferenceNavigationBuilder<TEntity, TRelatedEntity> HasOne<TRelatedEntity>(
            string? navigationName)
            where TRelatedEntity : class {
            return default;
        }

        public virtual ReferenceNavigationBuilder<TEntity, TRelatedEntity> HasOne<TRelatedEntity>(
            Expression<Func<TEntity, TRelatedEntity?>>? navigationExpression = null)
            where TRelatedEntity : class {
            return default;
        }

        public virtual CollectionNavigationBuilder<TEntity, TRelatedEntity> HasMany<TRelatedEntity>(
            string? navigationName)
            where TRelatedEntity : class {
            return default;
        }

        public virtual CollectionNavigationBuilder<TEntity, TRelatedEntity> HasMany<TRelatedEntity>(
            Expression<Func<TEntity, IEnumerable<TRelatedEntity>?>>? navigationExpression = null)
            where TRelatedEntity : class {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> HasChangeTrackingStrategy(
            ChangeTrackingStrategy changeTrackingStrategy) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> UsePropertyAccessMode(
            PropertyAccessMode propertyAccessMode) {
            return default;
        }

        public virtual DataBuilder<TEntity> HasData(params TEntity[] data) {
            return default;
        }

        public virtual DataBuilder<TEntity> HasData(IEnumerable<TEntity> data) {
            return default;
        }

        public virtual DataBuilder<TEntity> HasData(params object[] data) {
            return default;
        }

        public virtual DataBuilder<TEntity> HasData(IEnumerable<object> data) {
            return default;
        }

        public virtual DiscriminatorBuilder<TDiscriminator> HasDiscriminator<TDiscriminator>(
            Expression<Func<TEntity, TDiscriminator>> propertyExpression) {
            return default;
        }

        public virtual EntityTypeBuilder<TEntity> HasNoDiscriminator() {
            return default;
        }
    }

    public interface IMutableEntityType {
    }

    public class EntityTypeBuilder {
        public EntityTypeBuilder(IMutableEntityType entityType) {
        }

        public virtual IMutableEntityType Metadata { get; }

        public virtual EntityTypeBuilder HasAnnotation(string annotation, object? value) {
            return this;
        }

        public virtual EntityTypeBuilder HasBaseType(string? name) {
            return this;
        }

        public virtual EntityTypeBuilder HasBaseType(Type? entityType) {
            return this;
        }

        public virtual KeyBuilder HasKey(params string[] propertyNames) {
            return default;
        }

        public virtual KeyBuilder HasAlternateKey(params string[] propertyNames) {
            return default;
        }

        public virtual EntityTypeBuilder HasNoKey() {
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

        public virtual EntityTypeBuilder Ignore(string propertyName) {
            return default;
        }

        public virtual EntityTypeBuilder HasQueryFilter(LambdaExpression? filter) {
            return default;
        }

        public virtual IndexBuilder HasIndex(params string[] propertyNames) {
            return default;
        }

        public virtual IndexBuilder HasIndex(string[] propertyNames, string name) {
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

        public virtual EntityTypeBuilder OwnsOne(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder OwnsOne(
            string ownedTypeName,
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder OwnsOne(
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

        public virtual EntityTypeBuilder OwnsMany(
            string ownedTypeName,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder OwnsMany(
            string ownedTypeName,
            Type ownedType,
            string navigationName,
            Action<OwnedNavigationBuilder> buildAction) {
            return default;
        }

        public virtual EntityTypeBuilder OwnsMany(
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

        public virtual ReferenceNavigationBuilder HasOne(
            Type relatedType,
            string? navigationName = null) {
            return default;
        }

        public virtual ReferenceNavigationBuilder HasOne(string? navigationName) {
            return default;
        }

        protected virtual ForeignKey HasOneBuilder(
            MemberIdentity navigationId,
            EntityType relatedEntityType) {
            return default;
        }

        public virtual CollectionNavigationBuilder HasMany(
            string relatedTypeName,
            string? navigationName) {
            return default;
        }

        public virtual CollectionNavigationBuilder HasMany(
            string navigationName) {
            return default;
        }

        public virtual CollectionNavigationBuilder HasMany(
            Type relatedType,
            string? navigationName = null) {
            return default;
        }

        private CollectionNavigationBuilder HasMany(
            string? navigationName,
            EntityType relatedEntityType) {
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

        public virtual EntityTypeBuilder HasChangeTrackingStrategy(
            ChangeTrackingStrategy changeTrackingStrategy) {
            return default;
        }

        public virtual EntityTypeBuilder UsePropertyAccessMode(
            PropertyAccessMode propertyAccessMode) {
            return default;
        }

        public virtual DataBuilder HasData(params object[] data) {
            return default;
        }

        public virtual DataBuilder HasData(IEnumerable<object> data) {
            return default;
        }

        public virtual DiscriminatorBuilder HasDiscriminator() {
            return default;
        }

        public virtual DiscriminatorBuilder HasDiscriminator(string name, Type type) {
            return default;
        }

        public virtual DiscriminatorBuilder<TDiscriminator> HasDiscriminator<TDiscriminator>(
            string name) {
            return default;
        }

        public virtual EntityTypeBuilder HasNoDiscriminator() {
            return default;
        }
    }
}