using EntityFrameworkRuler.Editor.Controls;
using EntityFrameworkRuler.Editor.Dialogs;
using EntityFrameworkRuler.Generator;
using EntityFrameworkRuler.Saver;
using Microsoft.VisualStudio.PlatformUI;
// ReSharper disable MemberCanBePrivate.Global

namespace EntityFrameworkRuler.ToolWindows {
    public sealed partial class RulesFromEdmxDialog : DialogWindow, IRulesFromEdmxDialog {
        public RulesFromEdmxViewModel ViewModel { get; }

        public RulesFromEdmxDialog(ThemeNames? theme = null) : this(null, null) {
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
}
