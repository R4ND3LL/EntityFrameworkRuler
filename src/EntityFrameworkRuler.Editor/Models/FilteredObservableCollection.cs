using System.Collections;
using System.Collections.Specialized;

namespace EntityFrameworkRuler.Editor.Models {
    public sealed class FilteredObservableCollection<T> : IList, IList<T>, INotifyCollectionChanged, IDisposable {
        private readonly List<T> filteredList = new();
        private readonly IList<T> underlyingList;
        private bool isFiltering;
        private Predicate<T> filterPredicate;
        private readonly T defaultValue;
        private readonly bool isNullable;

        public FilteredObservableCollection(IList<T> underlyingList, Predicate<T> theFilterPredicate = null) {
            var t = typeof(T);
            defaultValue = (T)t.GetDefaultValue();
            isNullable = t.IsClass || t.IsNullableGenericType();
            if (underlyingList == null)
                throw new ArgumentNullException(nameof(underlyingList));
            if (underlyingList is not INotifyCollectionChanged nc)
                throw new ArgumentException("Underlying collection must implement INotifyCollectionChanged", nameof(underlyingList));
            if (underlyingList is not IList)
                throw new ArgumentException("Underlying collection must implement IList", nameof(underlyingList));
            this.underlyingList = underlyingList;
            nc.CollectionChanged += OnUnderlyingList_CollectionChanged;

            if (theFilterPredicate != null) Filter(theFilterPredicate);
        }

        private IComparer<T> sortComparer;
        public IComparer<T> SortComparer {
            get => sortComparer;
            set {
                if (sortComparer == value) return;
                sortComparer = value;
                if (filteredList?.Count > 0 && value != null) {
                    // must apply sort
                    filteredList.Sort(value);
                }
            }
        }
        public bool IsFixedSize => false;

        object IList.this[int index] { get => this[index]; set => throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public bool IsSynchronized => false;

        public object SyncRoot => isFiltering ? ((ICollection)filteredList).SyncRoot : ((ICollection)underlyingList).SyncRoot;

        public T this[int index] { get => isFiltering ? filteredList[index] : underlyingList[index]; set => throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public int Count => isFiltering ? filteredList.Count : underlyingList.Count;

        public bool IsReadOnly => true;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Add(object value) { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public bool Contains(object value) { return Contains((T)value); }

        public int IndexOf(object value) { return IndexOf((T)value); }

        public void Insert(int index, object value) { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public void Remove(object value) { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public void CopyTo(Array array, int index) {
            if (isFiltering) {
                if (array.Length - index < Count)
                    throw new ArgumentException("Array not big enough", nameof(array));
                var index1 = index;
                foreach (var filtered in filteredList) {
                    array.SetValue(filtered, index1);
                    ++index1;
                }
            } else
                ((ICollection)underlyingList).CopyTo(array, index);
        }

        public int IndexOf(T item) { return isFiltering ? filteredList.IndexOf(item) : underlyingList.IndexOf(item); }

        public void Insert(int index, T item) { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public void RemoveAt(int index) { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public void Add(T item) { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public void Clear() { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public bool Contains(T item) {
            if (isFiltering)
                return filteredList.Contains(item);
            return underlyingList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            if (isFiltering)
                filteredList.CopyTo(array, arrayIndex);
            else
                underlyingList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item) { throw new InvalidOperationException("FilteredObservableCollections are read-only"); }

        public IEnumerator<T> GetEnumerator() {
            if (isFiltering)
                return filteredList.GetEnumerator();
            return underlyingList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            if (isFiltering)
                return ((IEnumerable)filteredList).GetEnumerator();
            return underlyingList.GetEnumerator();
        }

        /// <summary> Rerun the filter predicate against all list items </summary>
        public void Refresh() {
            if (!isFiltering || filterPredicate == null) return;
            Filter(filterPredicate);
        }

        /// <summary> update the filtering for the individual items provided. this will add/remove items as necessary, and raise change events while doing it. </summary>
        public void Update(T item) { Update(new[] { item }); }

        /// <summary> update the filtering for the individual items provided. this will add/remove items as necessary, and raise change events while doing it. </summary>
        public void Update(IEnumerable<T> itemsToUpdate) {
            if (!isFiltering || filterPredicate == null) return;
            var list = itemsToUpdate as IList<T> ?? itemsToUpdate?.ToArray();
            if (list == null || list.Count == 0) return;
            var reset = (list.Count / (double)filteredList.Count) > 0.1;
            if (reset) BeginUpdate();
            try {
                UpdateFilteredItems(list);
            } finally {
                if (reset) EndUpdate();
            }
        }

        public void Filter(Predicate<T> theFilterPredicate) {
            filterPredicate = theFilterPredicate ?? throw new ArgumentNullException(nameof(theFilterPredicate));
            isFiltering = true;
            if (UpdateFilteredItems())
                RaiseCollectionChanged();
        }

        public void StopFiltering() {
            if (!isFiltering) return;
            filterPredicate = null;
            isFiltering = false;
            if (UpdateFilteredItems()) RaiseCollectionChanged();
        }

        // ReSharper disable once InconsistentNaming
        private void OnUnderlyingList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action == NotifyCollectionChangedAction.Reset) {
                if (UpdateFilteredItems()) RaiseCollectionChanged();
                return;
            }

            var newlist = e.NewItems?.OfType<T>().Where(CanShowItem).ToArray() ?? Array.Empty<T>();
            var oldlist = e.OldItems?.OfType<T>().ToArray() ?? Array.Empty<T>();

            if (newlist.Length > 0) {
                var last = filteredList.LastOrDefault();
                var lastUnderlyingIndex = filteredList.Count > 0 && !IsNull(last) ? underlyingList.IndexOf(last) : -1;
                var canAddToEndOfFilteredList = e.NewStartingIndex > lastUnderlyingIndex;
                if (!canAddToEndOfFilteredList) {
                    // order will be an issue. just reset it all
                    if (UpdateFilteredItems()) RaiseCollectionChanged();
                    return;
                }
            }

            // we can update the filtered list without resetting.
            var evnt = UpdateFilteredItemsForSingleEvent(newlist, oldlist);

            if (evnt == default(NotifyCollectionChangedEventArgs)) return;
            try {
                if (IsUpdateLocked)
                    isChanged = true;
                else
                    CollectionChanged?.Invoke(this, evnt);
            } catch (Exception ex) {
                Debug.WriteLine(ex, $"FilteredObservableCollection Changed event error: {ex.Message}");
                RaiseCollectionChanged();
            }
        }

        private bool CanShowItem(T item) { return !isFiltering || filterPredicate(item); }
        private bool IsNull(T item) { return isNullable && Equals(item, defaultValue); }

        private void RaiseCollectionChanged() {
            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }
        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args) {
            try {
                if (IsUpdateLocked)
                    isChanged = true;
                else
                    CollectionChanged?.Invoke(this, args);
            } catch {
                // ignored
            }
        }
        private bool UpdateFilteredItems() {
            filteredList.Clear();
            if (!isFiltering) return false;
            var sc = SortComparer;
            if (sc != null) {
                foreach (var underlying in underlyingList)
                    if (filterPredicate(underlying)) filteredList.AddSortedWithComparer(underlying, sc);
            } else {
                foreach (var underlying in underlyingList)
                    if (filterPredicate(underlying)) filteredList.Add(underlying);
            }
            return true;
        }
        /// <summary> update the filtering for the individual items provided. this will add/remove items as necessary, and raise change events while doing it. </summary>
        private bool UpdateFilteredItems(IEnumerable<T> itemsToUpdate) {
            if (!isFiltering) return false;
            var sc = SortComparer;
            var result = false;
            if (sc != null) {
                foreach (var underlying in itemsToUpdate) {
                    var i = filteredList.IndexOf(underlying);
                    if (filterPredicate(underlying)) {
                        if (i < 0) {
                            var start = filteredList.AddSortedWithComparer(underlying, sc);
                            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, new[] { underlying }, start));
                            result = true;
                        }
                    } else {
                        if (i >= 0) {
                            filteredList.RemoveAt(i);
                            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, new[] { underlying }, i));
                            result = true;
                        }
                    }
                }
            } else {
                foreach (var underlying in itemsToUpdate) {
                    var i = filteredList.IndexOf(underlying);
                    if (filterPredicate(underlying)) {
                        if (i < 0) {
                            filteredList.Add(underlying);
                            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, new[] { underlying }, i));
                            result = true;
                        }
                    } else {
                        if (i >= 0) {
                            filteredList.RemoveAt(i);
                            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, new[] { underlying }, i));
                            result = true;
                        }
                    }
                }
            }
            return result;
        }


        private NotifyCollectionChangedEventArgs UpdateFilteredItemsForSingleEvent(T[] newlist, T[] oldlist) {
            var newItemStartList = -1;
            var sc = SortComparer;
            Func<T, int> add = sc != null ? AddFilterItemSorted : AddFilterItem;
            foreach (var newItem in newlist) {
                var i = add(newItem);
                Debug.Assert(Equals(filteredList[i], newItem));
                if (newItemStartList == -1 || i < newItemStartList) newItemStartList = i;
            }

            var oldItemStartList = -1;
            var removed = new List<T>();
            foreach (var oldItem in oldlist) {
                var i = RemoveFilterItem(oldItem);
                if (i < 0) continue;
                removed.Add(oldItem);
                if (oldItemStartList == -1 || i < oldItemStartList) oldItemStartList = i;
            }

            if (newItemStartList >= 0 && removed.Count > 0) {
                // replace event
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newlist, removed, oldItemStartList);
                return e;
            }

            if (newItemStartList >= 0) {
                // add event
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newlist, newItemStartList);
                return e;
            }

            if (removed.Count > 0) {
                // remove event
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, oldItemStartList);
                return e;
            }

            return default;
            int AddFilterItem(T newItem) {
                filteredList.Add(newItem);
                return filteredList.Count - 1;
            }
            int AddFilterItemSorted(T newItem) {
                return filteredList.AddSortedWithComparer(newItem, sc);
            }
            int RemoveFilterItem(T oldItem) {
                var i = filteredList.IndexOf(oldItem);
                if (i >= 0) filteredList.RemoveAt(i);

                return i;
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose() {
            if (underlyingList is INotifyCollectionChanged nc) nc.CollectionChanged -= OnUnderlyingList_CollectionChanged;
            CollectionChanged = null;
        }

        #region lockable
        public bool IsUpdateLocked => updateLockCount > 0;
        private bool isChanged;
        private int updateLockCount;
        public void BeginUpdate() {
            if (!IsUpdateLocked) isChanged = false;
            updateLockCount++;
        }
        public void EndUpdate() {
            if (!IsUpdateLocked) return;
            updateLockCount--;
            if (IsUpdateLocked || !isChanged) return;
            try {
                CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
            } catch {
                // ignored
            }
        }
        #endregion
    }
}