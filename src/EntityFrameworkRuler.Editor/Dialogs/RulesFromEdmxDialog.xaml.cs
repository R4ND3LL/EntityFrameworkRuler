using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Saver;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Editor.Dialogs;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class RulesFromEdmxDialog : IRulesFromEdmxDialog {
    public RulesFromEdmxViewModel ViewModel { get; }

    public RulesFromEdmxDialog(RulesFromEdmxViewModel vm) {
        InitializeComponent();
        DataContext = ViewModel = vm;
        vm.OnGenerated = OnGenerated;
        if (!Theme.HasValue) Theme = ThemeNames.Light;
    }

    public ThemeNames? Theme {
        get => AppearanceManager.Current.SelectedTheme;
        set => AppearanceManager.Current.SelectedTheme = value;
    }

    private void OnGenerated(SaveRulesResponse response) {
        Tag = response;
        DialogResult = true;
        Close();
    }
}

