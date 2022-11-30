using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EntityFrameworkRuler.Common;
using EntityFrameworkRuler.Rules;

namespace EntityFrameworkRuler.Editor.Models;

public sealed partial class RuleNodeViewModel : NodeViewModel<RuleBase> {
    private readonly List<INotifyCollectionChanged> hookedCollections;

    public RuleNodeViewModel(RuleBase item, NodeViewModel<RuleBase> parent, TreeFilter filter = null, bool expand = true,
        RuleValidator validator = null) : base(item,
        parent, expand, filter) {
        Validator = validator ?? ((RuleNodeViewModel)parent)?.Validator ?? new RuleValidator();
        hookedCollections = new();
        HookCollectionChanges(item);
    }

    private void HookCollectionChanges(RuleBase item) {
        switch (item) {
            case DbContextRule dr:
                Hook(dr.Schemas);
                break;
            case SchemaRule sr:
                Hook(sr.Tables);
                break;
            case TableRule tr:
                Hook(tr.Columns);
                Hook(tr.Navigations);
                break;
            case ColumnRule cr:
                break;
            case NavigationRule nr:
                break;
        }

        void Hook(IEnumerable list) {
            if (list is not INotifyCollectionChanged cc) return;
            cc.CollectionChanged += OnItemCollectionChanged;
            hookedCollections.Add(cc);
        }
    }

    private void OnItemCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        // validate child contents
        var currentChildren = Item.GetChildren().ToArray();
        var nodeChildren = Children.Source;
        var checkAdded = e.Action.In(NotifyCollectionChangedAction.Reset, NotifyCollectionChangedAction.Add,
            NotifyCollectionChangedAction.Replace);
        var checkRemoved = e.Action.In(NotifyCollectionChangedAction.Reset, NotifyCollectionChangedAction.Remove,
            NotifyCollectionChangedAction.Replace);
        var altered = false;
        if (checkAdded)
            foreach (var child in currentChildren) {
                if (nodeChildren.Any(o => ReferenceEquals(o.Item, child))) continue;
                // doesnt exist. add it now
                nodeChildren.Add(new RuleNodeViewModel((RuleBase)child, this, Filter, false));
                altered = true;
            }

        if (checkRemoved) {
            // remove any child nodes that were deleted from the underlying collection
            var toRemove = nodeChildren.Where(o => currentChildren.All(c => !ReferenceEquals(c, o.Item))).ToArray();
            foreach (var node in toRemove) {
                nodeChildren.Remove(node);
                altered = true;
            }
        }

        if (altered) Children.Refresh();
    }

    public RuleValidator Validator { get; }
    public bool IsDbContext => Item is DbContextRule;
    public bool IsSchema => Item is SchemaRule;
    public bool IsTable => Item is TableRule;
    public bool IsColumn => Item is ColumnRule;

    public bool IsNavigation => Item is NavigationRule;
    [ObservableProperty] private IList<EvaluationFailure> errors;
    [ObservableProperty] private EvaluationFailure firstError;

    protected override ObservableCollection<NodeViewModel<RuleBase>> LoadChildren() {
        var collection = new ObservableCollection<NodeViewModel<RuleBase>>();
        var expChildren = IsDbContext || IsSchema;
        foreach (var child in Item.GetChildren())
            collection.Add(new RuleNodeViewModel((RuleBase)child, this, Filter, expChildren));
        return collection;
    }

    public override string Name {
        get => ((IRuleItem)Item)?.GetFinalName();
        set {
            ((IRuleItem)Item)?.SetFinalName(value);
            OnPropertiesChanged();
        }
    }

    public override IList<EvaluationFailure> Validate(bool withChildren = false) {
        var childHasError = false;
        var efs = new List<EvaluationFailure>();
        foreach (var childNode in Children.Source.Cast<RuleNodeViewModel>()) {
            if (withChildren)
                foreach (var error in childNode.Validate(true)) {
                    efs.Add(error);
                }

            if (childNode.HasError) childHasError = true;
        }

        foreach (var error in Validator.Validate(Item, false)) {
            efs.Add(error);
        }

        Errors = efs;
        HasError = childHasError || efs.Count > 0;
        FirstError = efs.FirstOrDefault();
        return efs;
    }

    public override NodeViewModel<RuleBase> AddChild() {
        if (!Item.CanHaveChildren()) return null;
        return null;
        //var cn = this.Node as CompositeNode;
        //if (cn == null) {
        //    return null;
        //}

        //var newChild = new CompositeNode() { Name = "New node" };
        //cn.Children.Add(newChild);
        //var vm = new NodeViewModel(newChild, this);
        //this.Children.Add(vm);
        //return vm;
    }

    protected override void OnEditingEnded() {
        base.OnEditingEnded();
        OnPropertiesChanged();
    }

    public override void OnKeyboardFocusChanged() {
        base.OnKeyboardFocusChanged();
        OnPropertiesChanged();
        foreach (var child in Children.Cast<RuleNodeViewModel>()) {
            child.OnPropertyChanged(nameof(Name));
        }
    }
}

public enum TreeFilterType {
    Contains,
    ExactMatch
}

public sealed partial class TreeFilter : ObservableObject {
    [ObservableProperty] private string term;
    [ObservableProperty] private TreeFilterType filterType;
}

public abstract partial class NodeViewModel<T> : ObservableObject {
    public sealed partial class TreeSelection : ObservableObject {
        [ObservableProperty] private NodeViewModel<T> node;
    }

    protected NodeViewModel(T item, NodeViewModel<T> parent, bool expand = true, TreeFilter filter = null,
        TreeSelection treeSelection = null) {
        Item = item;
        Parent = parent;
        IsExpanded = expand;
        this.filter = filter ?? parent?.filter ?? new TreeFilter();
        selection = treeSelection ?? parent?.Selection ?? new TreeSelection();
        filter.PropertyChanged += Filter_PropertyChanged;
    }

    private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e) {
        if (parent == null) ApplyFilter();
    }

    [ObservableProperty] private NodeViewModel<T> parent;
    [ObservableProperty] private bool isExpanded;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private int level;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private T item;
    [ObservableProperty] private TreeFilter filter;
    [ObservableProperty] private TreeSelection selection;

    public bool HasChildren => Children?.Count > 0;

    // ReSharper disable once InconsistentNaming
    protected FilteredObservableCollection<NodeViewModel<T>> children;
    public FilteredObservableCollection<NodeViewModel<T>> Children => children ??= new(LoadChildren(), TheFilterPredicate);
    public IList<NodeViewModel<T>> ChildrenUnfiltered => Children.Source;
    protected virtual bool CanFilter() => true;

    private bool TheFilterPredicate(NodeViewModel<T> n) {
        if (!CanFilter() || filter == null || filter.Term.IsNullOrWhiteSpace()) return true;
        return n.Children.Count > 0 || filter.FilterType switch {
            TreeFilterType.Contains => n.Name?.Contains((string)filter.Term, StringComparison.OrdinalIgnoreCase) == true,
            TreeFilterType.ExactMatch => n.Name?.EqualsIgnoreCase(filter.Term) == true,
            _ => n.Name?.EqualsIgnoreCase(filter.Term) == true
        };
    }

    public void Detach() {
        Parent.Children.Remove(this);
        Parent = null;
    }

    partial void OnIsEditingChanged(bool value) {
        if (!value) OnEditingEnded();
    }

    protected virtual void OnEditingEnded() { }
    public IEnumerable<T> ChildItems => Children?.Select(o => o.Item) ?? Enumerable.Empty<T>();

    protected abstract ObservableCollection<NodeViewModel<T>> LoadChildren();

    public virtual string Name { get => null; set { } }

    partial void OnIsSelectedChanged(bool value) {
        if (value) Selection.Node = this;
        else if (ReferenceEquals(Selection.Node, this)) Selection.Node = null;
    }

    partial void OnIsExpandedChanged(bool value) {
    }

    public virtual NodeViewModel<T> AddChild() => null;

    public void ExpandParents() {
        if (Parent == null) return;
        Parent.ExpandParents();
        Parent.IsExpanded = true;
    }

    public void ExpandAll() {
        IsExpanded = true;
        foreach (var child in Children) {
            child.ExpandAll();
        }
    }

    public void ApplyFilter() {
        foreach (var child in Children) {
            child.ApplyFilter();
        }

        Children.Refresh();
    }

    public abstract IList<EvaluationFailure> Validate(bool withChildren = false);

    public NodeViewModel<T> GetSelectedNode() {
        if (IsSelected) return this;
        foreach (var child in Children) {
            var n = child.GetSelectedNode();
            if (n != null) return n;
        }

        return null;
    }

    public IEnumerable<NodeViewModel<T>> GetSelectedNodes() {
        if (IsSelected) yield return this;
        foreach (var child in Children) {
            var n = child.GetSelectedNode();
            if (n != null) yield return n;
        }
    }

    public IEnumerable<NodeViewModel<T>> EnumerateParents(bool includeCurrent = true) {
        if (includeCurrent) yield return this;
        var p = parent;
        while (p != null) {
            yield return p;
            p = p.parent;
        }
    }

    /// <summary> raise property changed event for all properties </summary>
    internal virtual void OnPropertiesChanged() {
        OnPropertyChanged(string.Empty);
    }

    public virtual void OnKeyboardFocusChanged() { }

    public override string ToString() => Name;
}