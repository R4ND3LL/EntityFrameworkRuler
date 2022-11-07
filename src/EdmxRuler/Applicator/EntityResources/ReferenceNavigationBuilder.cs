
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection; 
// ReSharper disable CheckNamespace
namespace Microsoft.EntityFrameworkCore.Metadata.Builders {
    public class ReferenceNavigationBuilder<TEntity, TRelatedEntity> : ReferenceNavigationBuilder
        where TEntity : class
        where TRelatedEntity : class {
        public new virtual ReferenceCollectionBuilder<TRelatedEntity, TEntity> WithMany(
            string? navigationName = null)
            => default;

        public virtual ReferenceCollectionBuilder<TRelatedEntity, TEntity> WithMany(
            Expression<Func<TRelatedEntity, IEnumerable<TEntity>>>? navigationExpression)
            => default;

        public new virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> WithOne(
            string? navigationName = null)
            => default;

        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> WithOne(
            Expression<Func<TRelatedEntity, TEntity?>>? navigationExpression)
            => default;
    }

    public class ReferenceNavigationBuilder {
        protected virtual InternalForeignKeyBuilder Builder { [DebuggerStepThrough] get; }

        protected virtual string? ReferenceName { [DebuggerStepThrough] get; }

        protected virtual MemberInfo? ReferenceMember { [DebuggerStepThrough] get; }

        protected virtual IMutableEntityType RelatedEntityType { [DebuggerStepThrough] get; }

        protected virtual IMutableEntityType DeclaringEntityType { [DebuggerStepThrough] get; }

        public virtual ReferenceCollectionBuilder WithMany(string? collection = null) => default;
        public virtual ReferenceReferenceBuilder WithOne(string? reference = null) => default;
        protected virtual InternalForeignKeyBuilder WithOneBuilder(string? navigationName) => default;
    }

    public class ReferenceReferenceBuilder<TEntity, TRelatedEntity> : ReferenceReferenceBuilder
        where TEntity : class
        where TRelatedEntity : class {
        public new virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasAnnotation(
            string annotation,
            object? value)
            => default;

        public new virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey(
            string dependentEntityTypeName,
            params string[] foreignKeyPropertyNames)
            => default;

        public new virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey(
            Type dependentEntityType,
            params string[] foreignKeyPropertyNames)
            => default;

        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey<TDependentEntity>(
            params string[] foreignKeyPropertyNames)
            where TDependentEntity : class
            => default;

        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey<TDependentEntity>(
            Expression<Func<TDependentEntity, object?>> foreignKeyExpression)
            where TDependentEntity : class
            => default;

        public new virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey(
            string principalEntityTypeName,
            params string[] keyPropertyNames)
            => default;

        public new virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey(
            Type principalEntityType,
            params string[] keyPropertyNames)
            => default;

        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey<TPrincipalEntity>(
            params string[] keyPropertyNames)
            where TPrincipalEntity : class
            => default;

        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey<TPrincipalEntity>(
            Expression<Func<TPrincipalEntity, object?>> keyExpression)
            where TPrincipalEntity : class
            => default;

        public new virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> IsRequired(bool required = true)
            => default;

        public new virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> OnDelete(DeleteBehavior deleteBehavior)
            => default;
    }

    public enum DeleteBehavior { }

    public class ReferenceReferenceBuilder {
    }

    public class ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> : ReferenceCollectionBuilder
        where TPrincipalEntity : class
        where TDependentEntity : class {
        public new virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasAnnotation(
            string annotation,
            object? value)
            => default;

        public new virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasForeignKey(
            params string[] foreignKeyPropertyNames)
            => default;

        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasForeignKey(
            Expression<Func<TDependentEntity, object?>> foreignKeyExpression)
            => default;

        public new virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasPrincipalKey(
            params string[] keyPropertyNames)
            => default;

        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasPrincipalKey(
            Expression<Func<TPrincipalEntity, object?>> keyExpression)
            => default;

        public new virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> IsRequired(
            bool required = true)
            => default;

        public new virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> OnDelete(
            DeleteBehavior deleteBehavior)
            => default;
    }

    public class ReferenceCollectionBuilder {
    }
    
}