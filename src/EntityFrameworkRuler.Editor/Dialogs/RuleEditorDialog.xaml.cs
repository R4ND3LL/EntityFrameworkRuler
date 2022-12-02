using System.Windows;
using PropertyTools.Wpf;
using System.Windows.Input;
using System.Windows.Controls;
using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Models;
using EntityFrameworkRuler.Loader;
using EntityFrameworkRuler.Rules;
using EntityFrameworkRuler.Saver;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Editor.Dialogs;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class RuleEditorDialog {
    static RuleEditorDialog() {
        EventManager.RegisterClassHandler(typeof(Window), Keyboard.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(HandleGotKeyboardFocusEvent), true);
        RuleBase.Observable = true;
    }

    private static void HandleGotKeyboardFocusEvent(object sender, KeyboardFocusChangedEventArgs e) {
        if (e.OldFocus is not DependencyObject d) return;
        var parentWindow = GetWindow(d);
        if (parentWindow is not RuleEditorDialog re || re.ViewModel?.RootModel == null) return;
        var selection = re.ViewModel?.RootModel?.GetSelectedNode();
        if (selection == null) return;
        selection.OnKeyboardFocusChanged();
        Debug.WriteLine($"All properties changed raised for {selection.Name}");
    }

    public RuleEditorViewModel ViewModel { get; }

    public RuleEditorDialog() : this(null) {
    }

    public RuleEditorDialog(ThemeNames? theme) : this(null, null, null) {
        if (theme.HasValue) Theme = theme.Value;
        else if (!Theme.HasValue) Theme = ThemeNames.Light;
    }

    public RuleEditorDialog(IRuleLoader loader, IRuleSaver saver, string ruleFilePath, string targetProjectPath = null) {
        InitializeComponent();
#if DEBUG
        if (targetProjectPath.IsNullOrEmpty()) {
            var sln = Directory.GetCurrentDirectory().FindSolutionParentPath();
            if (sln != null) {
                sln = Path.Combine(sln, "Tests\\NorthwindTestProject\\");
                if (Directory.Exists(sln)) targetProjectPath = sln;
            }
        }
#endif
        DataContext = ViewModel = new(loader, saver, ruleFilePath, targetProjectPath);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public ThemeNames? Theme {
        get => AppearanceManager.Current.SelectedTheme;
        set => AppearanceManager.Current.SelectedTheme = value;
    }
}