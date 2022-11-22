using System.Windows;
using PropertyTools.Wpf;
using System.Windows.Input;
using System.Windows.Controls;
using EntityFrameworkRuler.Editor.Models;

namespace EntityFrameworkRuler.Editor.Dialogs;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class RuleEditorDialog : Window {
    private readonly RuleEditorViewModel vm;

    public RuleEditorDialog() : this(null, null) {
        Theme = ThemeNames.Light;
    }

    public RuleEditorDialog(string ruleFilePath, string targetProjectPath = null) {
        InitializeComponent();
        DataContext = vm = new RuleEditorViewModel(ruleFilePath, targetProjectPath);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public ThemeNames Theme {
        get => AppearanceManager.Current.SelectedTheme;
        set => AppearanceManager.Current.SelectedTheme = value;
    }

    void ModelBrowserKeyDown(object sender, KeyEventArgs e) {
        switch (e.Key) {
            case Key.F2:
            case Key.R when (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)):
                if (sender is TreeListBox tlb) {
                    if (tlb.SelectedItem is RuleNodeViewModel cvm) cvm.IsEditing = true;
                } else if (sender is TreeView tv) {
                    if (tv.SelectedItem is RuleNodeViewModel cvm) cvm.IsEditing = true;
                }
                break;
            case Key.Delete:
                Delete(null, null);
                break;
        }
    }

    private void Delete(object o, object o1) {

    }


    private void ContextMenu_Opened(object sender, RoutedEventArgs e) {
        var btn = sender as Button;
        var menu = sender as ContextMenu ?? btn?.ContextMenu;
        if (menu == null) return;
        var s = menu.Items.IndexOf(o => o is Separator);
        if (s < 0) return;
        while (menu.Items.Count > (s + 1)) menu.Items.RemoveAt(menu.Items.Count - 1);
        foreach (var ruleFile in vm.SuggestedRuleFiles) {
            var menuItem = new MenuItem() { Header = ruleFile.Path, Tag = ruleFile };
            menuItem.Click += MenuItem_Click;
            menu.Items.Add(menuItem);
        }
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e) {
        var menu = sender as MenuItem;
        if (menu?.Tag is not ObservableFileInfo file) return;
        vm.SelectedRuleFile = file;
    }

    private void ModelBrowserOnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        //if (Model?.TreeProcessor?.IsProcessing == true) return;
        //ScrollToSelected();
        //var node = (TimeCodeNode)e.NewValue;
        //if (Model != null) Model.CurrentTreeNode = node;
    }

    private void ModelBrowserOnLoaded(object sender, RoutedEventArgs e) { ScrollToSelected(); }

    private void ModelBrowserMouseMove(object sender, MouseEventArgs e) { ScrollToSelected(); }

    private void ModelBrowserTreeItemOnLoaded(object sender, RoutedEventArgs e) { ScrollToSelected(); }
    private bool scrolledOnce;
    private bool scrollingToSelected;
    private void ScrollToSelected() {
        if (!scrollingToSelected && !scrolledOnce && vm?.RootModel?.Children?.Count > 0) {
            scrollingToSelected = true;
            try {
                
                var node = vm.RootModel.GetSelectedNode();
                if (node == null) return;
                var item = (TreeViewItem)ModelBrowser.ItemContainerGenerator.ContainerFromItem(node);
                if (item == null) return;
                scrolledOnce = true;
                item.Focus();
                item.BringIntoView();
            } finally {
                scrollingToSelected = false;
            }
        }
    }


}