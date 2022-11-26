using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EntityFrameworkRuler.Editor.Dialogs;
using PropertyTools.Wpf;
using ColumnDefinition = PropertyTools.Wpf.ColumnDefinition;

namespace EntityFrameworkRuler.Editor.Controls;

public sealed class CustomControlFactory : PropertyGridControlFactory {
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomControlFactory"/> class.
    /// </summary>
    public CustomControlFactory() { }

    /// <inheritdoc />
    public override FrameworkElement CreateControl(PropertyItem property, PropertyControlFactoryOptions options) {
        var fe = base.CreateControl(property, options);
        if (property.Is(typeof(List<string>))) {
            if (fe is PropertyTools.Wpf.DataGrid dg) {
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = new(1, GridUnitType.Star) });
            }
        }

        if (fe is Control c) {
            if (fe is not PropertyTools.Wpf.DataGrid dg) {
                c.Background = AppearanceManager.Current.InputBackground;
            }
            c.Foreground = AppearanceManager.Current.InputText;
            var ff = AppearanceManager.Current.InputFontFamily;
            if (ff != null) c.FontFamily = ff;
        }
        return fe;
    }
    protected override FrameworkElement CreateGridControl(PropertyItem property) {
        var c = new MyDataGrid {
            CanDelete = property.ListCanRemove,
            CanInsert = property.ListCanAdd,
            InputDirection = property.InputDirection,
            IsEasyInsertByMouseEnabled = property.IsEasyInsertByMouseEnabled,
            IsEasyInsertByKeyboardEnabled = property.IsEasyInsertByKeyboardEnabled,
            AutoGenerateColumns = property.Columns.Count == 0
        };

        foreach (var cd in property.Columns) {
            if (cd.PropertyName == string.Empty && property.ListItemItemsSource != null) {
                cd.ItemsSource = property.ListItemItemsSource;
            }

            c.ColumnDefinitions.Add(cd);
        }

        c.SetBinding(PropertyTools.Wpf.DataGrid.ItemsSourceProperty, property.CreateBinding());
        //return c;
        {
            //dg.CanInsert = false;
            c.Background = AppearanceManager.Current.WindowBackground;
            c.Foreground = AppearanceManager.Current.InputText;
            c.BorderBrush = Brushes.Transparent;
            c.IsAutoFillEnabled = false;
            c.IsMoveAfterEnterEnabled = false;
            c.GridLineBrush = AppearanceManager.Current.GrayBrush8;
            //dg.Operator = new
        }
        return c;
    }
    /// <inheritdoc />
    public override ContentControl CreateErrorControl(PropertyItem pi, object instance, Tab tab, PropertyControlFactoryOptions options) {
        var dataErrorInfoInstance = instance as IDataErrorInfo;
        var notifyDataErrorInfoInstance = instance as INotifyDataErrorInfo;

        if (Application.Current.TryFindResource("ValidationErrorTemplateEx") != null) options.ValidationErrorTemplate = (DataTemplate)Application.Current.TryFindResource("ValidationErrorTemplateEx");

        var errorControl = new ContentControl {
            ContentTemplate = options.ValidationErrorTemplate,
            Focusable = false
        };

        IValueConverter errorConverter;
        string propertyPath;
        object source = null;
        if (dataErrorInfoInstance != null) {
            errorConverter = new DataErrorInfoConverter(dataErrorInfoInstance, pi.PropertyName);
            propertyPath = pi.PropertyName;
            source = instance;
        } else {
            errorConverter = new NotifyDataErrorInfoConverter(notifyDataErrorInfoInstance, pi.PropertyName);
            propertyPath = nameof(tab.HasErrors);
            source = tab;
            notifyDataErrorInfoInstance.ErrorsChanged += (s, e) => {
                UpdateTabForValidationResults(tab, notifyDataErrorInfoInstance);
                //needed to refresh error control's binding also when error changes (i.e from Error to Warning)
                errorControl.GetBindingExpression(ContentControl.ContentProperty).UpdateTarget();
            };

        }

        var visibilityBinding = new Binding(propertyPath) {
            Converter = errorConverter,
            NotifyOnTargetUpdated = true,
#if !NET40
            ValidatesOnNotifyDataErrors = false,
#endif
            Source = source,
        };

        var contentBinding = new Binding(propertyPath) {
            Converter = errorConverter,
#if !NET40
            ValidatesOnNotifyDataErrors = false,
#endif
            Source = source,
        };

        var warningBinding = new Binding(nameof(tab.HasWarnings)) {
            Converter = errorConverter,
#if !NET40
            ValidatesOnNotifyDataErrors = false,
#endif
            Source = source,
        };

        errorControl.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

        // When the visibility of the error control is changed, updated the HasErrors of the tab
        errorControl.TargetUpdated += (s, e) => {
            if (dataErrorInfoInstance != null)
                tab.UpdateHasErrors(dataErrorInfoInstance);
        };
        errorControl.SetBinding(ContentControl.ContentProperty, contentBinding);

        errorControl.SetBinding(ContentControl.ContentProperty, warningBinding);
        return errorControl;
    }

    /// <inheritdoc />
    public override void UpdateTabForValidationResults(Tab tab, object errorInfo) {
        if (errorInfo is INotifyDataErrorInfo ndei) {
            //tab.HasErrors = tab.Groups.Any(g => g.Properties.Any(p => ndei.GetErrors(p.PropertyName).Cast<object>()
            //    .Any(a => a != null && a.GetType() == typeof(ValidationResultE) && ((ValidationResultEx)a).Severity == Severity.Error)));

            //tab.HasWarnings = tab.Groups.Any(g => g.Properties.Any(p => ndei.GetErrors(p.PropertyName).Cast<object>()
            //    .Any(a => a != null && a.GetType() == typeof(ValidationResultEx) && ((ValidationResultEx)a).Severity == Severity.Warning)));
        } else if (errorInfo is IDataErrorInfo dei) tab.HasErrors = tab.Groups.Any(g => g.Properties.Any(p => !string.IsNullOrEmpty(dei[p.PropertyName])));
    }

    /// <inheritdoc />
    public override void SetValidationErrorStyle(FrameworkElement control, PropertyControlFactoryOptions options) {
        if (Application.Current.TryFindResource("ErrorInToolTipStyleEx") != null) {
            options.ValidationErrorStyle = (Style)Application.Current.TryFindResource("ErrorInToolTipStyleEx");
            control.Style = options.ValidationErrorStyle;
        }
    }
}

public sealed class MyDataGrid : PropertyTools.Wpf.DataGrid {
    protected override IDataGridOperator CreateOperator() {
        return new MyDataGridOperator(this, base.CreateOperator());
    }
}

public sealed class MyDataGridOperator : IDataGridOperator {
    private readonly MyDataGrid grid;
    private readonly IDataGridOperator op;

    public MyDataGridOperator(MyDataGrid grid, IDataGridOperator op) {
        this.grid = grid;
        this.op = op;
    }

    public void AutoGenerateColumns() {
        op.AutoGenerateColumns();
        var remove = grid.ColumnDefinitions
            .Where(o => o.Header is string s && (s.In("Tables", "Columns", "Properties", "Navigations", "UseSchemaName")
                                                 || s.Contains("Regex") || s.Contains("Replace")))
            .ToArray();
        remove.ForAll(o => grid.ColumnDefinitions.Remove(o));
    }

    public void UpdatePropertyDefinitions() {
        op.UpdatePropertyDefinitions();
    }

    public Type GetPropertyType(CellRef cell) {
        return op.GetPropertyType(cell);
    }

    public CellDescriptor CreateCellDescriptor(CellRef cell) {
        return op.CreateCellDescriptor(cell);
    }

    public string GetBindingPath(CellRef cell) {
        return op.GetBindingPath(cell);
    }

    public object GetCellValue(CellRef cell) {
        return op.GetCellValue(cell);
    }

    public int GetCollectionViewIndex(int index) {
        return op.GetCollectionViewIndex(index);
    }

    public int InsertItem(int index) {
        return op.InsertItem(index);
    }

    public object GetItem(CellRef cell) {
        return op.GetItem(cell);
    }

    public bool TrySetCellValue(CellRef cell, object value) {
        return op.TrySetCellValue(cell, value);
    }

    public object GetDataContext(CellRef cell) {
        return op.GetDataContext(cell);
    }

    public bool CanDeleteColumns() {
        return op.CanDeleteColumns();
    }

    public bool CanDeleteRows() {
        return op.CanDeleteRows();
    }

    public bool CanInsertColumns() {
        return op.CanInsertColumns();
    }

    public bool CanInsertRows() {
        return op.CanInsertRows();
    }

    public void DeleteColumns(int index, int n) {
        op.DeleteColumns(index, n);
    }

    public void DeleteRows(int index, int n) {
        op.DeleteRows(index, n);
    }

    public void InsertColumns(int index, int n) {
        op.InsertColumns(index, n);
    }

    public void InsertRows(int index, int n) {
        op.InsertRows(index, n);
    }

    public int GetRowCount() {
        return op.GetRowCount();
    }

    public int GetColumnCount() {
        return op.GetColumnCount();
    }

    public bool CanSort(int index) {
        return op.CanSort(index);
    }
}