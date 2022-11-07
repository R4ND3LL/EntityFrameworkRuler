﻿using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable CheckNamespace
namespace Microsoft.EntityFrameworkCore.ChangeTracking {
    public class ChangeTracker {
        public ChangeTracker(
            DbContext context,
            IStateManager stateManager,
            IChangeDetector changeDetector,
            IModel model,
            IEntityEntryGraphIterator graphIterator) {
        }

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
            TState? state,
            Func<EntityEntryGraphNode<TState>, bool> callback) {
        }

        public virtual void Clear() { }
        public virtual object DebugView => default;
    }

    public class EntityEntryGraphNode<TState> : EntityEntryGraphNode {
        public virtual TState? NodeState { get; set; }
    }

    public class EntityEntryGraphNode : IInfrastructure<InternalEntityEntry> {
        public virtual EntityEntry SourceEntry => default;
        public virtual INavigation InboundNavigation => default;
        public virtual object NodeState => default;
        public virtual EntityEntry Entry => default;
        InternalEntityEntry IInfrastructure<InternalEntityEntry>.Instance => default;

        public virtual EntityEntryGraphNode CreateNode(
            EntityEntryGraphNode currentNode,
            InternalEntityEntry internalEntityEntry,
            INavigation reachedVia) {
            return default;
        }
    }
}