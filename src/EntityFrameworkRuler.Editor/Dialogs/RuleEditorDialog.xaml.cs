using System.Windows;
using PropertyTools.Wpf;
using System.Windows.Input;
using System.Windows.Controls;
using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Models;
using EntityFrameworkRuler.Rules;

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
        if (parentWindow is not RuleEditorDialog re || re.vm?.RootModel == null) return;
        var selection = re.vm?.RootModel?.GetSelectedNode();
        if (selection == null) return;
        selection.OnKeyboardFocusChanged();
        Debug.WriteLine($"All properties changed raised for {selection.Name}");
    }

    private readonly RuleEditorViewModel vm;

    public RuleEditorDialog() : this(null) {
    }

    public RuleEditorDialog(ThemeNames? theme) : this(null, null) {
        if (theme.HasValue) Theme = theme.Value;
    }

    public RuleEditorDialog(string ruleFilePath, string targetProjectPath = null) {
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
        DataContext = vm = new(ruleFilePath, targetProjectPath);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public ThemeNames Theme {
        get => AppearanceManager.Current.SelectedTheme;
        set => AppearanceManager.Current.SelectedTheme = value;
    }

}