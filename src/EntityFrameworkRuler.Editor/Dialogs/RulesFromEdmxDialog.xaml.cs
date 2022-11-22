using EntityFrameworkRuler.Generator;

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
        DataContext = vm = new RulesFromEdmxViewModel(edmxFilePath, targetProjectPath, OnGenerated);
    }
    public ThemeNames Theme {
        get => AppearanceManager.Current.SelectedTheme;
        set => AppearanceManager.Current.SelectedTheme = value;
    }
    private void OnGenerated(SaveRulesResponse response) {
        this.Tag = response;
        this.DialogResult = true;
        this.Close();
    }
}