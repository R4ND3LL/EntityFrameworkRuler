 
using System;
using System.Collections.Generic;
using System.Linq.Expressions; 

// ReSharper disable CheckNamespace
namespace Microsoft.EntityFrameworkCore.Metadata.Builders {
    public class CollectionNavigationBuilder<TEntity, TRelatedEntity> : CollectionNavigationBuilder
        where TEntity : class
        where TRelatedEntity : class {
        public new virtual ReferenceCollectionBuilder<TEntity, TRelatedEntity> WithOne(
            string? navigationName = null)
            => default;

        public virtual ReferenceCollectionBuilder<TEntity, TRelatedEntity> WithOne(
            Expression<Func<TRelatedEntity, TEntity?>>? navigationExpression)
            => default;

        public new virtual CollectionCollectionBuilder<TRelatedEntity, TEntity> WithMany(string navigationName)
            => default;

        public virtual CollectionCollectionBuilder<TRelatedEntity, TEntity> WithMany(
            Expression<Func<TRelatedEntity, IEnumerable<TEntity>?>> navigationExpression)
            => default;
    }

    public class CollectionNavigationBuilder { }
    public class CollectionCollectionBuilder { }

    public class CollectionCollectionBuilder<TLeftEntity, TRightEntity> : CollectionCollectionBuilder
        where TLeftEntity : class
        where TRightEntity : class {
        public virtual EntityTypeBuilder<TJoinEntity> UsingEntity<TJoinEntity>()
            where TJoinEntity : class
            => default;

        public virtual EntityTypeBuilder<TJoinEntity> UsingEntity<TJoinEntity>(
            string joinEntityName)
            where TJoinEntity : class
            => default;

        public new virtual EntityTypeBuilder<TRightEntity> UsingEntity(
            Action<EntityTypeBuilder> configureJoinEntityType)
            => default;

        public new virtual EntityTypeBuilder<TRightEntity> UsingEntity(
            Type joinEntityType,
            Action<EntityTypeBuilder> configureJoinEntityType)
            => default;

        public new virtual EntityTypeBuilder<TRightEntity> UsingEntity(
            string joinEntityName,
            Action<EntityTypeBuilder> configureJoinEntityType)
            => default;

        public new virtual EntityTypeBuilder<TRightEntity> UsingEntity(
            string joinEntityName,
            Type joinEntityType,
            Action<EntityTypeBuilder> configureJoinEntityType)
            => default;

        public virtual EntityTypeBuilder<TRightEntity> UsingEntity<TJoinEntity>(
            Action<EntityTypeBuilder<TJoinEntity>> configureJoinEntityType)
            where TJoinEntity : class
            => default;

        public virtual EntityTypeBuilder<TRightEntity> UsingEntity<TJoinEntity>(
            string joinEntityName,
            Action<EntityTypeBuilder<TJoinEntity>> configureJoinEntityType)
            where TJoinEntity : class
            => default;

        public virtual EntityTypeBuilder<TJoinEntity> UsingEntity<TJoinEntity>(
            Func<EntityTypeBuilder<TJoinEntity>, ReferenceCollectionBuilder<TLeftEntity, TJoinEntity>> configureRight,
            Func<EntityTypeBuilder<TJoinEntity>, ReferenceCollectionBuilder<TRightEntity, TJoinEntity>> configureLeft)
            where TJoinEntity : class
            => default;

        public virtual EntityTypeBuilder<TJoinEntity> UsingEntity<TJoinEntity>(
            string joinEntityName,
            Func<EntityTypeBuilder<TJoinEntity>, ReferenceCollectionBuilder<TLeftEntity, TJoinEntity>> configureRight,
            Func<EntityTypeBuilder<TJoinEntity>, ReferenceCollectionBuilder<TRightEntity, TJoinEntity>> configureLeft)
            where TJoinEntity : class
            => default;

        public new virtual EntityTypeBuilder<TRightEntity> UsingEntity(
            Func<EntityTypeBuilder, ReferenceCollectionBuilder> configureRight,
            Func<EntityTypeBuilder, ReferenceCollectionBuilder> configureLeft,
            Action<EntityTypeBuilder> configureJoinEntityType)
            => default;

        public new virtual EntityTypeBuilder<TRightEntity> UsingEntity(
            Type joinEntityType,
            Func<EntityTypeBuilder, ReferenceCollectionBuilder> configureRight,
            Func<EntityTypeBuilder, ReferenceCollectionBuilder> configureLeft,
            Action<EntityTypeBuilder> configureJoinEntityType)
            => default;

        public new virtual EntityTypeBuilder<TRightEntity> UsingEntity(
            string joinEntityName,
            Func<EntityTypeBuilder, ReferenceCollectionBuilder> configureRight,
            Func<EntityTypeBuilder, ReferenceCollectionBuilder> configureLeft,
            Action<EntityTypeBuilder> configureJoinEntityType)
            => default;

        public new virtual EntityTypeBuilder<TRightEntity> UsingEntity(
            string joinEntityName,
            Type joinEntityType,
            Func<EntityTypeBuilder, ReferenceCollectionBuilder> configureRight,
            Func<EntityTypeBuilder, ReferenceCollectionBuilder> configureLeft,
            Action<EntityTypeBuilder> configureJoinEntityType)
            => default;

        public virtual EntityTypeBuilder<TRightEntity> UsingEntity<TJoinEntity>(
            Func<EntityTypeBuilder<TJoinEntity>, ReferenceCollectionBuilder<TLeftEntity, TJoinEntity>> configureRight,
            Func<EntityTypeBuilder<TJoinEntity>, ReferenceCollectionBuilder<TRightEntity, TJoinEntity>> configureLeft,
            Action<EntityTypeBuilder<TJoinEntity>> configureJoinEntityType)
            where TJoinEntity : class
            => default;

        public virtual EntityTypeBuilder<TRightEntity> UsingEntity<TJoinEntity>(
            string joinEntityName,
            Func<EntityTypeBuilder<TJoinEntity>, ReferenceCollectionBuilder<TLeftEntity, TJoinEntity>> configureRight,
            Func<EntityTypeBuilder<TJoinEntity>, ReferenceCollectionBuilder<TRightEntity, TJoinEntity>> configureLeft,
            Action<EntityTypeBuilder<TJoinEntity>> configureJoinEntityType)
            where TJoinEntity : class
            => default;
    }
}