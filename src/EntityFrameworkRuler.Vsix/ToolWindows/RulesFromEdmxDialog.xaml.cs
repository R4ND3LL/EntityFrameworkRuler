using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Dialogs;
using EntityFrameworkRuler.Saver;
using Microsoft.VisualStudio.PlatformUI;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.ToolWindows; 

public sealed partial class RulesFromEdmxDialog : DialogWindow, IRulesFromEdmxDialog {
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