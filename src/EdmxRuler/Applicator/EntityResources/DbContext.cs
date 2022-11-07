using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

// ReSharper disable CheckNamespace
namespace Microsoft.EntityFrameworkCore {
    public class DbContext :
        IDisposable,
        IAsyncDisposable {
        public DbContext(DbContextOptions options = null) { }

        public virtual DatabaseFacade Database => default;
        public virtual ChangeTracker ChangeTracker => default;
        public virtual IModel Model => default;
        public virtual DbContextId ContextId => default;

        public virtual DbSet<TEntity> Set<TEntity>() where TEntity : class {
            return default;
        }

        public virtual DbSet<TEntity> Set<TEntity>(string name) where TEntity : class {
            return default;
        }

        public virtual int SaveChanges() {
            return default;
        }

        public virtual int SaveChanges(bool acceptAllChangesOnSuccess) {
            return default;
        }

        public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            return default;
        }

        public virtual async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default) {
            return default;
        }

        public virtual void Dispose() {
        }

        public virtual ValueTask DisposeAsync() {
            return default;
        }

        public virtual EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class {
            return default;
        }

        public virtual EntityEntry Entry(object entity) {
            return default;
        }

        public virtual EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class {
            return default;
        }

        public virtual async ValueTask<EntityEntry<TEntity>> AddAsync<TEntity>(
            TEntity entity,
            CancellationToken cancellationToken = default)
            where TEntity : class {
            return default;
        }

        public virtual EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class {
            return default;
        }

        public virtual EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class {
            return default;
        }

        public virtual EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class {
            return default;
        }

        public virtual EntityEntry Add(object entity) {
            return default;
        }

        public virtual ValueTask<EntityEntry> AddAsync(
            object entity,
            CancellationToken cancellationToken = default) {
            return default;
        }

        public virtual EntityEntry Attach(object entity) {
            return default;
        }

        public virtual EntityEntry Update(object entity) {
            return default;
        }

        public virtual EntityEntry Remove(object entity) {
            return default;
        }

        public virtual void AddRange(params object[] entities) {
        }

        public virtual Task AddRangeAsync(params object[] entities) {
            return default;
        }

        public virtual void AttachRange(params object[] entities) {
        }

        public virtual void UpdateRange(params object[] entities) {
        }

        public virtual void RemoveRange(params object[] entities) {
        }

        public virtual void AddRange(IEnumerable<object> entities) {
        }

        public virtual Task AddRangeAsync(
            IEnumerable<object> entities,
            CancellationToken cancellationToken = default) {
            return default;
        }

        public virtual void AttachRange(IEnumerable<object> entities) { }

        public virtual void UpdateRange(IEnumerable<object> entities) { }

        public virtual void RemoveRange(IEnumerable<object> entities) { }

        public virtual object? Find(Type entityType, params object?[]? keyValues) {
            return default;
        }

        public virtual ValueTask<object?> FindAsync(Type entityType, params object?[]? keyValues) {
            return default;
        }

        public virtual ValueTask<object?> FindAsync(
            Type entityType,
            object?[]? keyValues,
            CancellationToken cancellationToken) {
            return default;
        }

        public virtual TEntity? Find<TEntity>(params object?[]? keyValues) where TEntity : class {
            return default;
        }

        public virtual ValueTask<TEntity?> FindAsync<TEntity>(params object?[]? keyValues) where TEntity : class {
            return default;
        }

        public virtual ValueTask<TEntity?> FindAsync<TEntity>(
            object?[]? keyValues,
            CancellationToken cancellationToken)
            where TEntity : class {
            return default;
        }

        public virtual IQueryable<TResult> FromExpression<TResult>(
            Expression<Func<IQueryable<TResult>>> expression) {
            return default;
        }
    }

    public readonly struct DbContextId {
        public DbContextId(Guid id, int lease) {
            InstanceId = id;
            Lease = lease;
        }

        public Guid InstanceId { get; }
        public int Lease { get; }
    }
}