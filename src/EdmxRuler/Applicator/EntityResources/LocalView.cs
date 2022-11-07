using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.ChangeTracking {
    public class LocalView<TEntity> :
        ICollection<TEntity>,
        INotifyCollectionChanged,
        INotifyPropertyChanged,
        INotifyPropertyChanging,
        IListSource
        where TEntity : class {
        public virtual ObservableCollection<TEntity> ToObservableCollection()
            => default;

        public virtual IEnumerator<TEntity> GetEnumerator() => default;
        public virtual void Add(TEntity item) { }

        public virtual void Clear() {
        }

        public virtual bool Contains(TEntity item) => default;

        public virtual void CopyTo(TEntity[] array, int arrayIndex) {
        }

        public virtual bool Remove(TEntity item) => default;
        public virtual int Count => default;
        public virtual bool IsReadOnly => default;
        IList IListSource.GetList() => default;

        bool IListSource.ContainsListCollection
            => false;

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;
    }
}