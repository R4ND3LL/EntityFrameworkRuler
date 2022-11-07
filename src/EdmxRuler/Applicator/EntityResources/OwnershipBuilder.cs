using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Metadata.Builders; 

public abstract class InternalForeignKeyBuilder {
}

public abstract class RelationshipBuilderBase {
}

public class OwnershipBuilder<TEntity, TDependentEntity> : OwnershipBuilder
    where TEntity : class
    where TDependentEntity : class {
    public OwnershipBuilder(
        IMutableEntityType principalEntityType,
        IMutableEntityType dependentEntityType,
        IMutableForeignKey foreignKey)
        : base(principalEntityType, dependentEntityType, foreignKey) {
    }

    public virtual OwnershipBuilder<TEntity, TDependentEntity> HasAnnotation(
        string annotation,
        object? value) {
        return default;
    }

    public virtual OwnershipBuilder<TEntity, TDependentEntity> HasForeignKey(
        params string[] foreignKeyPropertyNames) {
        return default;
    }

    public virtual OwnershipBuilder<TEntity, TDependentEntity> HasForeignKey(
        Expression<Func<TDependentEntity, object>> foreignKeyExpression) {
        return default;
    }

    public virtual OwnershipBuilder<TEntity, TDependentEntity> HasPrincipalKey(
        params string[] keyPropertyNames) {
        return default;
    }

    public virtual OwnershipBuilder<TEntity, TDependentEntity> HasPrincipalKey(
        Expression<Func<TEntity, object?>> keyExpression) {
        return default;
    }
}

public class OwnershipBuilder : RelationshipBuilderBase {
    public OwnershipBuilder(
        IMutableEntityType principalEntityType,
        IMutableEntityType dependentEntityType,
        IMutableForeignKey foreignKey) {
    }

    public virtual OwnershipBuilder HasAnnotation(string annotation, object? value) {
        return default;
    }

    public virtual OwnershipBuilder HasForeignKey(
        params string[] foreignKeyPropertyNames) {
        return default;
    }

    public virtual OwnershipBuilder HasPrincipalKey(params string[] keyPropertyNames) {
        return default;
    }
}