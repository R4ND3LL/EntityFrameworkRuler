using EntityFrameworkRuler.Saver;

namespace EntityFrameworkRuler.Editor.Controls;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class RulesFromEdmxView {
    public RulesFromEdmxView() => InitializeComponent();

    public ThemeNames? Theme {
        get => AppearanceManager.Current.SelectedTheme;
        set => AppearanceManager.Current.SelectedTheme = value;
    }
}