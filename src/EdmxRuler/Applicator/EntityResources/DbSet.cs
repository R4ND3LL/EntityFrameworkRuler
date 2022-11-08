using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

// ReSharper disable CheckNamespace

namespace Microsoft.EntityFrameworkCore {
    public interface IEntityType {
    }

    public abstract class DbSet<TEntity> :
        IQueryable<TEntity>,
        IEnumerable<TEntity>,
        IEnumerable,
        IQueryable
        where TEntity : class {
        /// <summary>
        ///     The <see cref="T:Microsoft.EntityFrameworkCore.Metadata.IEntityType" /> metadata associated with this set.
        /// </summary>
        public abstract IEntityType EntityType { get; }

        /// <summary>
        ///     Returns this object typed as <see cref="T:System.Collections.Generic.System.Collections.Generic.IAsyncEnumerable`1" />.
        /// </summary>
        /// <remarks>
        ///     See <see href="https://aka.ms/efcore-docs-query">Querying data with EF Core</see> for more information.
        /// </remarks>
        /// <returns>This object.</returns>
        public virtual IAsyncEnumerable<TEntity> AsAsyncEnumerable() {
            return (IAsyncEnumerable<TEntity>)this;
        }

        public virtual IQueryable<TEntity> AsQueryable() {
            return (IQueryable<TEntity>)this;
        }

        public virtual LocalView<TEntity> Local => throw new NotSupportedException();

        public virtual TEntity Find(params object[] keyValues) {
            throw new NotSupportedException();
        }

        public virtual ValueTask<TEntity> FindAsync(params object[] keyValues) {
            throw new NotSupportedException();
        }

        public virtual ValueTask<TEntity> FindAsync(
            object[] keyValues,
            CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        public virtual EntityEntry<TEntity> Add(TEntity entity) {
            throw new NotSupportedException();
        }

        public virtual ValueTask<EntityEntry<TEntity>> AddAsync(
            TEntity entity,
            CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public virtual EntityEntry<TEntity> Attach(TEntity entity) {
            throw new NotSupportedException();
        }

        public virtual EntityEntry<TEntity> Remove(TEntity entity) {
            throw new NotSupportedException();
        }

        public virtual EntityEntry<TEntity> Update(TEntity entity) {
            throw new NotSupportedException();
        }

        public virtual void AddRange(params TEntity[] entities) {
            throw new NotSupportedException();
        }

        public virtual Task AddRangeAsync(params TEntity[] entities) {
            throw new NotSupportedException();
        }

        public virtual void AttachRange(params TEntity[] entities) {
            throw new NotSupportedException();
        }

        public virtual void RemoveRange(params TEntity[] entities) {
            throw new NotSupportedException();
        }

        public virtual void UpdateRange(params TEntity[] entities) {
            throw new NotSupportedException();
        }

        public virtual void AddRange(IEnumerable<TEntity> entities) {
            throw new NotSupportedException();
        }

        public virtual Task AddRangeAsync(
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public virtual void AttachRange(IEnumerable<TEntity> entities) {
            throw new NotSupportedException();
        }

        public virtual void RemoveRange(IEnumerable<TEntity> entities) {
            throw new NotSupportedException();
        }

        public virtual void UpdateRange(IEnumerable<TEntity> entities) {
            throw new NotSupportedException();
        }

        public virtual IAsyncEnumerator<TEntity> GetAsyncEnumerator(
            CancellationToken cancellationToken = default) {
            return ((IAsyncEnumerable<TEntity>)this).GetAsyncEnumerator(cancellationToken);
        }

        Type IQueryable.ElementType => throw new NotSupportedException();
        Expression IQueryable.Expression => throw new NotSupportedException();
        IQueryProvider IQueryable.Provider => throw new NotSupportedException();
        public IEnumerator<TEntity> GetEnumerator() { throw new NotImplementedException(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}