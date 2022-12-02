using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Saver;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.Editor.Dialogs;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class RulesFromEdmxDialog {
    public RulesFromEdmxViewModel ViewModel { get; }

    public RulesFromEdmxDialog(ThemeNames? theme) : this(null, null) {
        if (theme.HasValue) Theme = theme.Value;
        else if (!Theme.HasValue) Theme = ThemeNames.Light;
    }

    public RulesFromEdmxDialog(IRuleGenerator generator, string edmxFilePath, string targetProjectPath = null) {
        InitializeComponent();
        DataContext = ViewModel = new(generator, edmxFilePath, targetProjectPath, OnGenerated);
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