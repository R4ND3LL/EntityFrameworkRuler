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
public sealed partial class RuleEditorDialog : Window, IRuleEditorDialog {
    public RuleEditorViewModel ViewModel { get; }

    public RuleEditorDialog(RuleEditorViewModel vm) { 
        InitializeComponent(); 
        DataContext = ViewModel = vm;
        if (!Theme.HasValue) Theme = ThemeNames.Light;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public ThemeNames? Theme {
        get => AppearanceManager.Current.SelectedTheme;
        set => AppearanceManager.Current.SelectedTheme = value;
    }
}

