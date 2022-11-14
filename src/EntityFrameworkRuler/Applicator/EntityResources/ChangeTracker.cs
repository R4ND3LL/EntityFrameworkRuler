// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.ChangeTracking {
    public class ChangeTracker {
        public ChangeTracker(DbContext context) { }

        public virtual bool AutoDetectChangesEnabled { get; set; } = true;

        public virtual bool LazyLoadingEnabled { get; set; } = true;

        public virtual QueryTrackingBehavior QueryTrackingBehavior {
            get => default;
            set { }
        }

        public virtual IEnumerable<EntityEntry> Entries() {
            yield break;
        }

        public virtual IEnumerable<EntityEntry<TEntity>> Entries<TEntity>() where TEntity : class {
            yield break;
        }

        public virtual bool HasChanges() {
            return default;
        }

        public virtual DbContext Context { get; }

        public virtual void DetectChanges() {
        }

        public virtual void AcceptAllChanges() { }
        public virtual void TrackGraph(object rootEntity, Action<EntityEntryGraphNode> callback) { }

        public virtual void TrackGraph<TState>(
            object rootEntity,
            TState state,
            Func<EntityEntryGraphNode<TState>, bool> callback) {
        }

        public virtual void Clear() { }
        public virtual object DebugView => default;
    }

    public enum QueryTrackingBehavior { }

    public class EntityEntryGraphNode<TState> : EntityEntryGraphNode {
        public virtual TState NodeState { get; set; }
    }

    public class EntityEntryGraphNode {
        public virtual EntityEntry SourceEntry => default;
        public virtual INavigation InboundNavigation => default;
        public virtual object NodeState => default;
        public virtual EntityEntry Entry => default;

        public virtual EntityEntryGraphNode CreateNode(
            EntityEntryGraphNode currentNode,
            object internalEntityEntry,
            INavigation reachedVia) {
            return default;
        }
    }

    public interface INavigation {
    }
}