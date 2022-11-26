using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Saver;

namespace EntityFrameworkRuler.Editor.Dialogs;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class RulesFromEdmxDialog {
    private readonly RulesFromEdmxViewModel vm;

    public RulesFromEdmxDialog() : this(null, null) {
    }

    public RulesFromEdmxDialog(string edmxFilePath, string targetProjectPath = null) {
        InitializeComponent();
        DataContext = vm = new(edmxFilePath, targetProjectPath, OnGenerated);
    }
    public ThemeNames Theme {
        get => AppearanceManager.Current.SelectedTheme;
        set => AppearanceManager.Current.SelectedTheme = value;
    }
    private void OnGenerated(SaveRulesResponse response) {
        Tag = response;
        DialogResult = true;
        Close();
    }

}