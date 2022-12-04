using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Dialogs;
using Microsoft.VisualStudio.PlatformUI;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.ToolWindows {
    public sealed partial class RuleEditorDialog : DialogWindow, IRuleEditorDialog {
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
}
